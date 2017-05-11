﻿using ReactiveUI;
using MagnumOpus.Models;
using MagnumOpus.Services.ActiveDirectoryServices;
using MagnumOpus.Services.NavigationServices;
using System;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Threading.Tasks;

namespace MagnumOpus.ViewModels
{
	public class UserWindowViewModel : ViewModelBase, INavigable
	{
		public UserWindowViewModel()
		{
			SetUser = ReactiveCommand.CreateFromObservable<string, UserObject>(identity => ActiveDirectoryService.Current.GetUser(identity));

            _user = SetUser
                .ToProperty(this, x => x.User);

            this.WhenActivated(disposables =>
            {
                SetUser
                    .ThrownExceptions
                    .SelectMany(ex => _messages.Handle(new MessageInfo(MessageType.Error, ex.Message, "Could not load user")))
                    .Subscribe()
                    .DisposeWith(disposables);
            });
		}



		public ReactiveCommand<string, UserObject> SetUser { get; private set; }
        public UserObject User => _user.Value;



		public Task OnNavigatedTo(object parameter)
		{
			if (parameter is string s)
			{
				Observable.Return(s)
					.InvokeCommand(SetUser);
			}
			else if (parameter is Tuple<string,string> t)
			{
				Observable.Return(t.Item1)
					.InvokeCommand(SetUser);

                Observable.Return(t.Item2)
                    .Delay(TimeSpan.FromSeconds(1))
                    .ObserveOnDispatcher()
                    .Subscribe(x => MessageBus.Current.SendMessage(x, ApplicationActionRequest.SetLocalProfileComputerName));
			}

			return Task.FromResult<object>(null);
		}

		public Task OnNavigatingFrom() => Task.FromResult<object>(null);



        private readonly ObservableAsPropertyHelper<UserObject> _user;
    }
}
