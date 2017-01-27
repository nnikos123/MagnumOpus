﻿using ReactiveUI;
using SupportTool.Models;
using System.Reactive;
using System;

namespace SupportTool.ViewModels
{
	public class ViewModelBase : ReactiveObject, ISupportsActivation
	{
        protected readonly ViewModelActivator _activator = new ViewModelActivator();
		protected readonly Interaction<MessageInfo, int> _promptMessages = new Interaction<MessageInfo, int>();
		protected readonly Interaction<MessageInfo, Unit> _infoMessages = new Interaction<MessageInfo, Unit>();
		protected readonly Interaction<MessageInfo, Unit> _errorMessages = new Interaction<MessageInfo, Unit>();
		protected readonly Interaction<DialogInfo, Unit> _dialogRequests = new Interaction<DialogInfo, Unit>();



        public ViewModelActivator Activator => _activator;

        public Interaction<MessageInfo, int> PromptMessages => _promptMessages;

		public Interaction<MessageInfo, Unit> InfoMessages => _infoMessages;

		public Interaction<MessageInfo, Unit> ErrorMessages => _errorMessages;

		public Interaction<DialogInfo, Unit> DialogRequests => _dialogRequests;
    }
}
