﻿using ReactiveUI;
using SupportTool.Models;
using SupportTool.Services.DialogServices;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reactive;
using System.Reactive.Linq;
using System.Text;
using System.Threading.Tasks;
using static System.Net.Mime.MediaTypeNames;

namespace SupportTool.ViewModels
{
    public class RemotePanelViewModel : ReactiveObject
    {
        private readonly ReactiveCommand<Unit, Unit> _openLoggedOn;
        private readonly ReactiveCommand<Unit, Unit> _openLoggedOnPlus;
        private readonly ReactiveCommand<Unit, Unit> _openRemoteExecution;
        private readonly ReactiveCommand<Unit, Unit> _openCDrive;
        private readonly ReactiveCommand<Unit, Unit> _startRemoteControl;
        private ComputerObject _computer;



        public RemotePanelViewModel()
        {
            _openLoggedOn = ReactiveCommand.Create(() => ExecuteCmd(@"C:\PsTools\PsLoggedon.exe", $@"\\{_computer.CN}"));

            _openLoggedOnPlus = ReactiveCommand.Create(()=> ExecuteCmd(@"C:\PsTools\PsExec.exe", $@"\\{_computer.CN} C:\Windows\System32\cmd.exe /K query user"));

            _openRemoteExecution = ReactiveCommand.Create(() => ExecuteCmd(@"C:\PsTools\PsExec.exe", $@"\\{_computer.CN} C:\Windows\System32\cmd.exe"));

            _openCDrive = ReactiveCommand.Create(() =>
            {
                Process.Start($@"\\{_computer.CN}\C$");
            });
            _openCDrive
                .ThrownExceptions
                .Subscribe(ex => DialogService.ShowError(ex.Message, "Could not open location"));

            _startRemoteControl = ReactiveCommand.Create(() => StartRemoteControlImpl(_computer));

            Observable.Merge(
                _openLoggedOn.ThrownExceptions,
                _openRemoteExecution.ThrownExceptions,
                _startRemoteControl.ThrownExceptions)
                .Subscribe(ex => DialogService.ShowError(ex.Message, "Could not launch external program"));
        }



        public ReactiveCommand OpenLoggedOn => _openLoggedOn;

        public ReactiveCommand OpenLoggedOnPlus => _openLoggedOnPlus;

        public ReactiveCommand OpenRemoteExecution => _openRemoteExecution;

        public ReactiveCommand OpenCDrive => _openCDrive;

        public ReactiveCommand StartRemoteControl => _startRemoteControl;

        public ComputerObject Computer
        {
            get { return _computer; }
            set { this.RaiseAndSetIfChanged(ref _computer, value); }
        }



        private void ExecuteCmd(string fileName, string arguments = "") => ExecuteFile(@"C:\Windows\System32\cmd.exe", $@"/K {fileName} {arguments}");

        private void ExecuteFile(string fileName, string arguments = "")
        {
            if (File.Exists(fileName)) Process.Start(fileName, arguments);
            else throw new ArgumentException($"Could not find {fileName}");
        }

        private void StartRemoteControlImpl(ComputerObject computer)
        {
            var fileName = @"C:\SCCM Remote Control\rc.exe";
            var arguments = $"1 {computer.CN}";

            if (computer.Company == "SIHF"
                || computer.Company == "REV")
            {
                fileName = @"C:\RemoteControl2012\CmRcViewer.exe";
                arguments = computer.CN;
            }

            ExecuteFile(fileName, arguments);
        }
    }
}