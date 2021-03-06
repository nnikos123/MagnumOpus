﻿using ReactiveUI;
using System.Reactive.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Reactive.Disposables;
using MagnumOpus.Dialog;

namespace MagnumOpus.Computer
{
    /// <summary>
    /// Interaction logic for PingPanel.xaml
    /// </summary>
    public partial class PingPanel : UserControl, IViewFor<PingPanelViewModel>
    {
        public PingPanel()
        {
            InitializeComponent();

            ViewModel = new PingPanelViewModel();

            this.WhenActivated(d =>
            {
                this.Bind(ViewModel, vm => vm.HostName, v => v.HostName).DisposeWith(d);

                this.Bind(ViewModel, vm => vm.IsPinging, v => v.PingToggleButton.IsChecked).DisposeWith(d);
                this.OneWayBind(ViewModel, vm => vm.IsPinging, v => v.PingToggleButton.Content, isPinging => !isPinging ? "Start" : "Stop").DisposeWith(d);
                this.OneWayBind(ViewModel, vm => vm.MostRecentPingResult, v => v.PingResultTextBlock.Text).DisposeWith(d);
                this.OneWayBind(ViewModel, vm => vm.IsPinging, v => v.PingResultDetailsToggleButton.IsEnabled).DisposeWith(d);
                this.Bind(ViewModel, vm => vm.IsShowingPingResultDetails, v => v.PingResultDetailsToggleButton.IsChecked).DisposeWith(d);
                this.OneWayBind(ViewModel, vm => vm.IsShowingPingResultDetails, v => v.PingResultDetailsStackPanel.Visibility).DisposeWith(d);
                this.OneWayBind(ViewModel, vm => vm.PingResults, v => v.PingResultDetailsListView.ItemsSource).DisposeWith(d);

                ViewModel
                    .WhenAnyValue(vm => vm.IsPinging)
                    .Where(true)
                    .ToSignal()
                    .ObserveOnDispatcher()
                    .InvokeCommand(ViewModel.StartPing).DisposeWith(d);
                ViewModel
                    .WhenAnyValue(vm => vm.IsPinging)
                    .Where(false)
                    .ToSignal()
                    .ObserveOnDispatcher()
                    .InvokeCommand(ViewModel.StopPing).DisposeWith(d);
                ViewModel
                    .WhenAnyValue(vm => vm.HostName)
                    .WhereNotNull()
                    .ToSignal()
                    .ObserveOnDispatcher()
                    .InvokeCommand(ViewModel.StopPing).DisposeWith(d);
            });
        }

        public Interaction<MessageInfo, int> Messages => ViewModel.Messages;

        public string HostName { get => (string)GetValue(HostNameProperty); set => SetValue(HostNameProperty, value); }
        public static readonly DependencyProperty HostNameProperty = DependencyProperty.Register(nameof(HostName), typeof(string), typeof(PingPanel), new PropertyMetadata(null));

        public PingPanelViewModel ViewModel { get => (PingPanelViewModel)GetValue(ViewModelProperty); set => SetValue(ViewModelProperty, value); }
        public static readonly DependencyProperty ViewModelProperty = DependencyProperty.Register(nameof(ViewModel), typeof(PingPanelViewModel), typeof(PingPanel), new PropertyMetadata(null));

        object IViewFor.ViewModel { get => ViewModel; set => ViewModel = value as PingPanelViewModel; }
    }
}
