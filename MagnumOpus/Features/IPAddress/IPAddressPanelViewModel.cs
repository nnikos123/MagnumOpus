﻿using ReactiveUI;
using System;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Reactive;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using MagnumOpus.Dialog;
using static MagnumOpus.FileHelpers.ExecutionService;

namespace MagnumOpus.IPAddress
{
    public class IPAddressPanelViewModel : ViewModelBase
	{
		public IPAddressPanelViewModel()
		{
			OpenLoggedOn = ReactiveCommand.Create(() =>
			{
				//EnsureFileIsAvailable("PsLoggedon.exe");
				//ExecuteCmd(Path.Combine(Directory.GetCurrentDirectory(), "PsLoggedon.exe"), $@"\\{_ipAddress}");
			});

			OpenLoggedOnPlus = ReactiveCommand.Create(() =>
			{
				//EnsureFileIsAvailable("PsExec.exe");
				//ExecuteCmd(Path.Combine(Directory.GetCurrentDirectory(), "PsExec.exe"), $@"\\{_ipAddress} C:\Windows\System32\cmd.exe /K query user");
			});

			OpenRemoteExecution = ReactiveCommand.Create(() =>
			{
				//EnsureFileIsAvailable("PsExec.exe");
				//ExecuteCmd(Path.Combine(Directory.GetCurrentDirectory(), "PsExec.exe"), $@"\\{_ipAddress} C:\Windows\System32\cmd.exe");
			});

			OpenCDrive = ReactiveCommand.Create(() => { Process.Start($@"\\{_ipAddress}\C$"); });

			RebootComputer = ReactiveCommand.CreateFromObservable(() => _messages.Handle(new MessageInfo(MessageType.Question, $"Reboot {_ipAddress}?", "", "Yes", "No"))
                .Where(result => result == 0)
                .Do(_ => RunFile(Path.Combine(System32Path, "shutdown.exe"), $@"-r -f -m \\{_ipAddress} -t 0"))
                .ToSignal()
			);

            StartRemoteControl = ReactiveCommand.Create(() => { });// ExecuteFile(SettingsService.Current.RemoteControlClassicPath, $"1 {_ipAddress}"));

            StartRemoteControl2012 = ReactiveCommand.Create(() => { });// ExecuteFile(SettingsService.Current.RemoteControl2012Path, _ipAddress));

            KillRemoteControl = ReactiveCommand.Create(() => RunFile(Path.Combine(System32Path, "taskkill.exe"), $"/s {_ipAddress} /im rcagent.exe /f"));

			StartRemoteAssistance = ReactiveCommand.Create(() => RunFile(Path.Combine(System32Path, "msra.exe"), $"/offerra {_ipAddress}"));

			StartRdp = ReactiveCommand.Create(() => RunFile(Path.Combine(System32Path, "mstsc.exe"), $"/v {_ipAddress}"));

			_hostName = this.WhenAnyValue(vm => vm.IPAddress)
				.Where(ipAddress => ipAddress.HasValue())
				.Select(ipAddress => Dns.GetHostEntry(ipAddress).HostName)
				.CatchAndReturn("")
				.ToProperty(this, vm => vm.HostName);

            this.WhenActivated(disposables =>
            {
	            OpenCDrive
		            .ThrownExceptions
		            .SelectMany(ex => _messages.Handle(new MessageInfo(MessageType.Error, ex.Message, "Could not open location")))
		            .Subscribe()
		            .DisposeWith(disposables);

	            Observable.Merge(
			            OpenLoggedOn.ThrownExceptions,
			            OpenLoggedOnPlus.ThrownExceptions,
			            OpenRemoteExecution.ThrownExceptions,
			            RebootComputer.ThrownExceptions,
			            StartRemoteControl.ThrownExceptions,
			            StartRemoteControl2012.ThrownExceptions,
			            KillRemoteControl.ThrownExceptions,
			            StartRemoteAssistance.ThrownExceptions,
			            StartRdp.ThrownExceptions)
		            .SelectMany(ex => _messages.Handle(new MessageInfo(MessageType.Error, ex.Message, "Could not launch external program")))
		            .Subscribe()
		            .DisposeWith(disposables);
            });
		}



		public ReactiveCommand<Unit, Unit> OpenLoggedOn { get; }
		public ReactiveCommand<Unit, Unit> OpenLoggedOnPlus { get; }
        public ReactiveCommand<Unit, Unit> OpenRemoteExecution { get; }
        public ReactiveCommand<Unit, Unit> OpenCDrive { get; }
        public ReactiveCommand<Unit, Unit> RebootComputer { get; }
        public ReactiveCommand<Unit, Unit> StartRemoteControl { get; }
        public ReactiveCommand<Unit, Unit> StartRemoteControl2012 { get; }
        public ReactiveCommand<Unit, Unit> KillRemoteControl { get; }
        public ReactiveCommand<Unit, Unit> StartRemoteAssistance { get; }
        public ReactiveCommand<Unit, Unit> StartRdp { get; }
        public string HostName => _hostName.Value;
        public string IPAddress { get => _ipAddress; set => this.RaiseAndSetIfChanged(ref _ipAddress, value); }



        private readonly ObservableAsPropertyHelper<string> _hostName;
        private string _ipAddress;
    }
}
