﻿using Newtonsoft.Json;
using System;
using System.IO;
using System.Reactive;
using System.Reactive.Linq;

namespace MagnumOpus.Services.FileServices
{
	public class FileService
	{
		public static readonly string LocalAppData = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Magnum Opus");
		public static readonly string AppData = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "Magnum Opus");



		public static IObservable<Unit> WriteToLocalAppData(string key, string data) => Observable.Start(() =>
		{
			Directory.CreateDirectory(LocalAppData);
			File.WriteAllText(Path.Combine(LocalAppData, $"{key}.txt"), data);
		});

		public static IObservable<Unit> WriteToAppData(string key, string data) => Observable.Start(() =>
		{
			Directory.CreateDirectory(AppData);
			File.WriteAllText(Path.Combine(AppData, $"{key}.txt"), data);
		});

		public static IObservable<string> ReadFromLocalAppData(string key) => Observable.Start(() => File.ReadAllText(Path.Combine(LocalAppData, $"{key}.txt")));

		public static IObservable<string> ReadFromAppData(string key) => Observable.Start(() => File.ReadAllText(Path.Combine(AppData, $"{key}.txt")));



		public static IObservable<Unit> SerializeToDisk<T>(string key, T data) => WriteToLocalAppData(key, JsonConvert.SerializeObject(data));

		public static IObservable<T> DeserializeFromDisk<T>(string key) => Observable.Start(() => JsonConvert.DeserializeObject<T>(ReadFromLocalAppData(key).Wait()));
	}
}