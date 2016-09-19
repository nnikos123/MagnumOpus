﻿using ReactiveUI;
using SupportTool.ViewModels;
using System;
using System.Collections.Generic;
using System.Linq;
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
	/// Interaction logic for IPAddressWindow.xaml
	/// </summary>
	public partial class IPAddressWindow : Window, IViewFor<IPAddressWindowViewModel>
	{
		public IPAddressWindow()
		{
			InitializeComponent();

			this.OneWayBind(ViewModel, vm => vm.IPAddress, v => v.Title, x => x ?? "");
			this.OneWayBind(ViewModel, vm => vm.IPAddress, v => v.IPAddressPanel.IPAddress);
		}

		public IPAddressWindowViewModel ViewModel
		{
			get { return (IPAddressWindowViewModel)GetValue(ViewModelProperty); }
			set { SetValue(ViewModelProperty, value); }
		}

		public static readonly DependencyProperty ViewModelProperty = DependencyProperty.Register(nameof(ViewModel), typeof(IPAddressWindowViewModel), typeof(IPAddressWindow), new PropertyMetadata(new IPAddressWindowViewModel()));

		object IViewFor.ViewModel
		{
			get { return ViewModel; }
			set { ViewModel = value as IPAddressWindowViewModel; }
		}
	}
}