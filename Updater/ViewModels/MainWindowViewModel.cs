﻿using Microsoft.Win32;
using ReactiveUI;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reactive;
using System.Reactive.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Updater.ViewModels
{
	public class MainWindowViewModel : ViewModelBase
	{
		public MainWindowViewModel()
		{
			_browseForSourceFile = ReactiveCommand.Create(BrowseForSourceFileImpl);
			_browseForSourceFile
				.Where(x => x.HasValue())
				.Subscribe(x => SourceFilePath = x);

			_browseForDestinationFolder = ReactiveCommand.Create(BrowseForDestinationFolderImpl);
			_browseForDestinationFolder
				.Where(x => x.HasValue())
				.Subscribe(x => DestinationFolderPath = x);

			_addDestinationFolder = ReactiveCommand.Create(
				() => _destinationFolders.Add(_destinationFolderPath),
				this.WhenAnyValue(x => x.DestinationFolderPath, x => x.HasValue()));
			_addDestinationFolder
				.Subscribe(_ => DestinationFolderPath = "");

			_confirm = ReactiveCommand.CreateFromObservable(() => ConfirmImpl(_sourceFilePath, _destinationFolders));

			_destinationFoldersSortedView = _destinationFolders.CreateDerivedCollection(x => x, orderer: (x, y) => x.CompareTo(y));
		}



		public ReactiveCommand BrowseForSourceFile => _browseForSourceFile;

		public ReactiveCommand BrowseForDestinationFolder => _browseForDestinationFolder;

		public ReactiveCommand AddDestinationFolder => _addDestinationFolder;

		public ReactiveCommand RemoveDestinationFolder => _removeDestinationFolder;

		public ReactiveCommand Confirm => _confirm;

		public ReactiveList<string> DestinationFolders => _destinationFolders;

		public IReactiveDerivedList<string> DestinationFoldersSortedView => _destinationFoldersSortedView;

		public string SourceFilePath
		{
			get { return _sourceFilePath; }
			set { this.RaiseAndSetIfChanged(ref _sourceFilePath, value); }
		}

		public string DestinationFolderPath
		{
			get { return _destinationFolderPath; }
			set { this.RaiseAndSetIfChanged(ref _destinationFolderPath, value); }
		}



		private string BrowseForSourceFileImpl()
		{
			var dialog = new Microsoft.Win32.OpenFileDialog();
			if (dialog.ShowDialog() == true)
			{
				return dialog.FileName;
			}

			return "";
		}

		private string BrowseForDestinationFolderImpl()
		{
			var dialog = new FolderBrowserDialog();
			if (dialog.ShowDialog() == DialogResult.OK)
			{
				return dialog.SelectedPath;
			}

			return "";
		}

		private IObservable<bool> ConfirmImpl(string sourceFilePath, IEnumerable<string> destinationFolders) => Observable.Start(() =>
		{
			var file = new FileInfo(sourceFilePath);
			if (!file.Exists) throw new ArgumentException($"File \"{sourceFilePath}\" not found");

			foreach (var directoryPath in destinationFolders)
			{
				var directory = new DirectoryInfo(directoryPath);
				if (!directory.Exists) continue;

				file.CopyTo(Path.Combine(directory.FullName, file.Name), true);
			}

			return true;
		});



		private readonly ReactiveCommand<Unit, string> _browseForSourceFile;
		private readonly ReactiveCommand<Unit, string> _browseForDestinationFolder;
		private readonly ReactiveCommand<Unit, Unit> _addDestinationFolder;
		private readonly ReactiveCommand<Unit, Unit> _removeDestinationFolder;
		private readonly ReactiveCommand<Unit, bool> _confirm;
		private readonly ReactiveList<string> _destinationFolders = new ReactiveList<string>();
		private readonly IReactiveDerivedList<string> _destinationFoldersSortedView;
		private string _sourceFilePath;
		private string _destinationFolderPath;
	}
}
