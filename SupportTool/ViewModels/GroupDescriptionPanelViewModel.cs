﻿using ReactiveUI;
using SupportTool.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive;
using System.Reactive.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SupportTool.ViewModels
{
	public class GroupDescriptionPanelViewModel : ViewModelBase
	{
		public GroupDescriptionPanelViewModel()
		{
            _enableEditing = ReactiveCommand.Create<Unit, bool>(_ => { _descriptionBackup = _description; return true; });

            _save = ReactiveCommand.CreateFromObservable<Unit, bool>(_ => SaveImpl(_group, _description).Select(x => false));

			_cancel = ReactiveCommand.Create<Unit, bool>(_ => { Description = _descriptionBackup; return false; });

			_isEditingEnabled = Observable.Merge(
                _enableEditing,
                _save,
                _cancel)
				.ToProperty(this, x => x.IsEditingEnabled);

            this.WhenAnyValue(x => x.Group.Principal.Description)
                .WhereNotNull()
                .Subscribe(x => Description = x);

			Observable.Merge(
                _enableEditing.ThrownExceptions,
				_save.ThrownExceptions,
				_cancel.ThrownExceptions)
				.Subscribe(async ex => await _errorMessages.Handle(new MessageInfo(ex.Message)));
		}



        public ReactiveCommand EnabledEditing => _enableEditing;

		public ReactiveCommand Save => _save;

		public ReactiveCommand Cancel => _cancel;

		public bool IsEditingEnabled => _isEditingEnabled.Value;

		public GroupObject Group
		{
			get { return _group; }
			set { this.RaiseAndSetIfChanged(ref _group, value); }
		}

		public string Description
		{
			get { return _description; }
			set { this.RaiseAndSetIfChanged(ref _description, value); }
		}



		private IObservable<Unit> SaveImpl(GroupObject group, string description) => Observable.Start(() =>
		{
			group.Principal.Description = description.HasValue() ? description : null;
			group.Principal.Save();
		});



        private readonly ReactiveCommand<Unit, bool> _enableEditing;
		private readonly ReactiveCommand<Unit, bool> _save;
		private readonly ReactiveCommand<Unit, bool> _cancel;
		private readonly ObservableAsPropertyHelper<bool> _isEditingEnabled;
		private GroupObject _group;
		private string _description;
		private string _descriptionBackup;
	}
}