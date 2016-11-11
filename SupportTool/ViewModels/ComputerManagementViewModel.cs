﻿using ReactiveUI;
using SupportTool.Models;
using SupportTool.Services.DialogServices;
using SupportTool.Services.FileServices;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reactive;
using System.Reactive.Linq;
using System.Text;
using System.Threading.Tasks;

using static SupportTool.Executables.Helpers;
using static SupportTool.Services.FileServices.ExecutionService;

namespace SupportTool.ViewModels
{
	public class ComputerManagementViewModel : ReactiveObject
	{
		private ReactiveCommand<Unit, Unit> _rebootComputer;
		private ReactiveCommand<Unit, Unit> _runPSExec;
		private ReactiveCommand<Unit, Unit> _openCDrive;
		private ReactiveCommand<Unit, Unit> _openSccm;
		private ComputerObject _computer;



		public ComputerManagementViewModel()
		{
			_rebootComputer = ReactiveCommand.Create(() =>
			{
				if (DialogService.ShowPrompt($"Reboot {_computer.CN}?"))
				{
					ExecuteFile(@"C:\Windows\System32\shutdown.exe", $@"-r -f -m \\{_computer.CN} -t 0");
				}
			});

			_runPSExec = ReactiveCommand.Create(() =>
			{
				EnsureExecutableIsAvailable("PsExec.exe");
				ExecuteCmd($"\"{Path.Combine(FileService.LocalAppData, "PsExec.exe")}\"", $@"\\{_computer.CN} C:\Windows\System32\cmd.exe");
			});

			_openCDrive = ReactiveCommand.Create(() => { Process.Start($@"\\{_computer.CN}\C$"); });

			_openSccm = ReactiveCommand.Create(() => { ExecuteFile(@"C:\Program Files\SCCM Tools\SCCM Client Center\SMSCliCtrV2.exe", _computer.CN); });

			Observable.Merge(
				_rebootComputer.ThrownExceptions,
				_runPSExec.ThrownExceptions,
				_openCDrive.ThrownExceptions,
				_openSccm.ThrownExceptions)
				.Subscribe(ex => DialogService.ShowError(ex.Message));
		}



		public ReactiveCommand RebootComputer => _rebootComputer;

		public ReactiveCommand RunPSExec => _runPSExec;

		public ReactiveCommand OpenCDrive => _openCDrive;

		public ReactiveCommand OpenSccm => _openSccm;

		public ComputerObject Computer
		{
			get { return _computer; }
			set { this.RaiseAndSetIfChanged(ref _computer, value); }
		}
	}
}