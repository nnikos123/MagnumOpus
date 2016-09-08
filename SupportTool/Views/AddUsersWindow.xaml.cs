﻿using ReactiveUI;
using SupportTool.Helpers;
using SupportTool.ViewModels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive;
using System.Reactive.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace SupportTool.Views
{
	/// <summary>
	/// Interaction logic for AddUsersWindow.xaml
	/// </summary>
	public partial class AddUsersWindow : Window, IViewFor<AddUsersWindowViewModel>
	{
		public AddUsersWindow()
		{
			InitializeComponent();

			this.Bind(ViewModel, vm => vm.SearchString, v => v.SearchTextBox.Text);
			this.OneWayBind(ViewModel, vm => vm.SearchResultsView, v => v.SearchResultsListView.ItemsSource);
			this.Bind(ViewModel, vm => vm.SelectedSearchResult, v => v.SearchResultsListView.SelectedItem);
			this.OneWayBind(ViewModel, vm => vm.UsersToAddView, v => v.UsersToAddListView.ItemsSource);
			this.Bind(ViewModel, vm => vm.SelectedUserToAdd, v => v.UsersToAddListView.SelectedItem);

			this.WhenActivated(d =>
			{
				SearchTextBox.Focus();

				d(this.BindCommand(ViewModel, vm => vm.AddToUsersToAdd, v => v.SearchResultsListView, nameof(ListView.MouseDoubleClick)));
				d(this.BindCommand(ViewModel, vm => vm.AddToUsersToAdd, v => v.AddToUsersToAddButton));
				d(this.BindCommand(ViewModel, vm => vm.RemoveFromUsersToAdd, v => v.UsersToAddListView, nameof(ListView.MouseDoubleClick)));
				d(this.BindCommand(ViewModel, vm => vm.RemoveFromUsersToAdd, v => v.RemoveFromUsersToAddButton));
				d(this.BindCommand(ViewModel, vm => vm.Save, v => v.SaveButton));

				d(ViewModel
					.WhenAnyValue(x => x.SearchString)
					.Throttle(TimeSpan.FromMilliseconds(500))
					.Where(x => x.HasValue())
					.Select(_ => Unit.Default)
					.ObserveOnDispatcher()
					.InvokeCommand(ViewModel, x => x.SearchForUsers));
			});
		}

		public AddUsersWindowViewModel ViewModel
		{
			get { return (AddUsersWindowViewModel)GetValue(ViewModelProperty); }
			set { SetValue(ViewModelProperty, value); }
		}

		public static readonly DependencyProperty ViewModelProperty = DependencyProperty.Register(nameof(ViewModel), typeof(AddUsersWindowViewModel), typeof(AddUsersWindow), new PropertyMetadata(new AddUsersWindowViewModel()));

		object IViewFor.ViewModel
		{
			get { return ViewModel; }
			set { ViewModel = value as AddUsersWindowViewModel; }
		}
	}
}
