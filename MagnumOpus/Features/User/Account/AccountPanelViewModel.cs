﻿using ReactiveUI;
using System;
using System.Diagnostics;
using System.Reactive;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Reactive.Concurrency;
using MagnumOpus.ActiveDirectory;
using MagnumOpus.Computer;
using MagnumOpus.Dialog;
using MagnumOpus.FileHelpers;
using MagnumOpus.Settings;
using Splat;

namespace MagnumOpus.User
{
    public class AccountPanelViewModel : ViewModelBase
    {
        public AccountPanelViewModel()
        {
            SetNewPassword = ReactiveCommand.CreateFromObservable(
                () => SetNewPasswordImpl(_newPassword),
                this.WhenAnyValue(vm => vm.User, vm => vm.NewPassword, (user, password) => user != null && password.HasValue()));

            SetNewSimplePassword = ReactiveCommand.CreateFromObservable(SetNewSimplePasswordImpl);

            SetNewComplexPassword = ReactiveCommand.CreateFromObservable(SetNewComplexPasswordImpl);

            ExpirePassword = ReactiveCommand.CreateFromObservable(() => Messages.Handle(new MessageInfo(MessageType.Question, "Are you sure you want to expire the password?", "Expire password?", "Yes", "No"))
                .Where(result => result == 0)
                .SelectMany(_ => _adFacade.ExpirePassword(User.Principal.SamAccountName)));

            UnlockAccount = ReactiveCommand.CreateFromObservable(() => _adFacade.UnlockUser(User.Principal.SamAccountName));

            RunLockoutStatus = ReactiveCommand.Create(() => ExecutionService.RunFileFromCache("LockoutStatus", "LockoutStatus.exe", $"-u:{_adFacade.CurrentDomain}\\{User.Principal.SamAccountName}"));

            OpenPermittedWorkstations = ReactiveCommand.CreateFromObservable(() => _dialogRequests.Handle(new DialogInfo(new PermittedWorkstationsDialog(), _user.Principal.SamAccountName)));

            ToggleEnabled = ReactiveCommand.CreateFromObservable(() => _adFacade.SetEnabled(User.Principal.SamAccountName, !User.Principal.Enabled ?? true));

            OpenSplunk = ReactiveCommand.Create(() =>
            {
                Process.Start(string.Format(Locator.Current.GetService<SettingsFacade>().SplunkUrl, _adFacade.CurrentDomainShortName, User.Principal.SamAccountName));
            });

            this.WhenActivated(disposables =>
            {
                SetNewPassword
                    .ObserveOnDispatcher()
                    .Do(_ => NewPassword = "")
                    .SelectMany(newPass => _messages.Handle(new MessageInfo(MessageType.Success, $"New password is: {newPass}", "Password set")))
                    .Subscribe()
                    .DisposeWith(disposables);

                SetNewSimplePassword
                    .ObserveOnDispatcher()
                    .SelectMany(newPass => _messages.Handle(MessageInfo.PasswordSetMessageInfo(newPass)))
                    .Subscribe()
                    .DisposeWith(disposables);

                SetNewComplexPassword
                    .ObserveOnDispatcher()
                    .SelectMany(newPass => _messages.Handle(MessageInfo.PasswordSetMessageInfo(newPass)))
                    .Subscribe()
                    .DisposeWith(disposables);

                ExpirePassword
                    .SelectMany(_ => _messages.Handle(new MessageInfo(MessageType.Success, "User must change password at next login", "Password expired")))
                    .Subscribe()
                    .DisposeWith(disposables);

                UnlockAccount
                    .SelectMany(_ =>
                    {
                        MessageBus.Current.SendMessage(_user.CN, ApplicationActionRequest.Refresh);
                        return _messages.Handle(new MessageInfo(MessageType.Success, "Account unlocked"));
                    })
                    .Subscribe()
                    .DisposeWith(disposables);

                ToggleEnabled
                    .Subscribe(_ => MessageBus.Current.SendMessage(_user.CN, ApplicationActionRequest.Refresh))
                    .DisposeWith(disposables);


                Observable.Merge(
                    SetNewPassword.ThrownExceptions,
                    SetNewSimplePassword.ThrownExceptions,
                    SetNewComplexPassword.ThrownExceptions)
                    .SelectMany(ex => _messages.Handle(MessageInfo.PasswordSetErrorMessageInfo(ex.Message)))
                    .Subscribe()
                    .DisposeWith(disposables);


                Observable.Merge<(string Title, string Message)>(
                        ExpirePassword.ThrownExceptions.Select(ex => ("Could not expire password", ex.Message)),
                        UnlockAccount.ThrownExceptions.Select(ex => ("Could not unlock acount", ex.Message)),
                        RunLockoutStatus.ThrownExceptions.Select(ex => ("Could not open LockOutStatus", ex.Message)),
                        OpenPermittedWorkstations.ThrownExceptions.Select(ex => ("Could not open Permitted Workstations", ex.Message)),
                        ToggleEnabled.ThrownExceptions.Select(ex => ("Could not toggle enabled status", ex.Message)))
                    .SelectMany(dialogContent => _messages.Handle(new MessageInfo(MessageType.Error, dialogContent.Message, dialogContent.Title)))
                    .Subscribe()
                    .DisposeWith(disposables);
            });
        }



        public ReactiveCommand<Unit, string> SetNewPassword { get; }
        public ReactiveCommand<Unit, string> SetNewSimplePassword { get; }
        public ReactiveCommand<Unit, string> SetNewComplexPassword { get; }
        public ReactiveCommand<Unit, Unit> ExpirePassword { get; }
        public ReactiveCommand<Unit, Unit> UnlockAccount { get; }
        public ReactiveCommand<Unit, Unit> RunLockoutStatus { get; }
        public ReactiveCommand<Unit, Unit> OpenPermittedWorkstations { get; }
        public ReactiveCommand<Unit, Unit> ToggleEnabled { get; }
        public ReactiveCommand<Unit, Unit> OpenSplunk { get; }
        public UserObject User { get => _user; set => this.RaiseAndSetIfChanged(ref _user, value); }
        public bool IsShowingNewPasswordOptions { get => _isShowingNewPasswordOptions; set => this.RaiseAndSetIfChanged(ref _isShowingNewPasswordOptions, value); }
        public string NewPassword { get => _newPassword; set => this.RaiseAndSetIfChanged(ref _newPassword, value); }



        private IObservable<string> SetNewPasswordImpl(string newPassword) => Observable.Return(newPassword)
            .SelectMany(password => _adFacade.SetPassword(User.Principal.SamAccountName, password, false, TaskPoolScheduler.Default).Select(_ => password));

        private IObservable<string> SetNewSimplePasswordImpl() => Observable.Return($"{DateTimeOffset.Now.DayOfWeek.ToNorwegianString()}{DateTimeOffset.Now.Minute:00}")
            .SelectMany(password => _adFacade.SetPassword(User.Principal.SamAccountName, password).Select(_ => password));

        private IObservable<string> SetNewComplexPasswordImpl() => Observable.Start(() =>
        {
            var possibleChars = "abcdefgijkmnopqrstwxyzABCDEFGHJKLMNPQRSTWXYZ23456789*$-+?_&=!%{}/";
            var randGen = new Random(DateTime.Now.Second);
            var password = "";
            for (var i = 0; i < 16; i++) password += possibleChars[randGen.Next(possibleChars.Length)];
            return password;
        }, TaskPoolScheduler.Default)
        .SelectMany(password => _adFacade.SetPassword(User.Principal.SamAccountName, password, scheduler: CurrentThreadScheduler.Instance).Select(_ => password));



        private readonly ADFacade _adFacade = Locator.Current.GetService<ADFacade>();
        private UserObject _user;
        private bool _isShowingNewPasswordOptions;
        private string _newPassword;
    }
}
