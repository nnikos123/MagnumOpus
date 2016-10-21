﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Resources;

namespace SupportTool.Executables
{
	public static class Helpers
	{
		public static void WriteExecutableToDisk(string executable)
		{
			var rStream = Application.GetResourceStream(new Uri($"pack://application:,,,/Executables/{executable}"));
			using (var fs = new FileStream(executable, FileMode.Create, FileAccess.Write))
			using (var stream = new MemoryStream())
			using (var writer = new BinaryWriter(fs))
			{
				rStream.Stream.CopyTo(stream);
				writer.Write(stream.ToArray());
			}
		}

		public static void EnsureExecutableIsAvailable(string executable)
		{
			if (!File.Exists(executable))
			{
				WriteExecutableToDisk(executable);
			}
		}
	}
}