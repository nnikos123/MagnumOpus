﻿using Akavache;
using System;
using System.Reactive.Linq;

namespace MagnumOpus.State
{
	public class StateService
	{
		public static IObservable<T> Get<T>(string key, T defaultValue = default(T)) => BlobCache.LocalMachine.GetObject<T>(key).CatchAndReturn(defaultValue);

		public async static void Set<T>(string key, T value) => await BlobCache.LocalMachine.InsertObject(key, value);
	}
}
