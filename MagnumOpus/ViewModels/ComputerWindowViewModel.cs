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
	public class ComputerWindowViewModel : ViewModelBase, INavigable
	{
		public ComputerWindowViewModel()
		{
			_setComputer = ReactiveCommand.CreateFromObservable<string, ComputerObject>(identity => ActiveDirectoryService.Current.GetComputer(identity));

            _computer = _setComputer
				.ToProperty(this, x => x.Computer);

            this.WhenActivated(disposables =>
            {
                _setComputer
                .ThrownExceptions
                .Subscribe(async ex => await _errorMessages.Handle(new MessageInfo(ex.Message)))
                .DisposeWith(disposables);
            });
		}



		public ReactiveCommand SetComputer => _setComputer;

		public ComputerObject Computer => _computer.Value;



		public Task OnNavigatedTo(object parameter)
		{
			if (parameter is string)
			{
				Observable.Return(parameter as string)
					.InvokeCommand(_setComputer);
			}

			return Task.FromResult<object>(null);
		}

		public Task OnNavigatingFrom() => Task.FromResult<object>(null);



        private readonly ReactiveCommand<string, ComputerObject> _setComputer;
        private readonly ObservableAsPropertyHelper<ComputerObject> _computer;
    }
}