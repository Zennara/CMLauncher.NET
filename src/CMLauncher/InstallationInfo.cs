using System;
using System.IO;

namespace CMLauncher
{
	public class InstallationInfo
	{
		public string GameKey { get; set; } = string.Empty;
		public string Name { get; set; } = string.Empty;
		public string Version { get; set; } = string.Empty;
		public DateTime? Timestamp { get; set; }
		public string RootPath { get; set; } = string.Empty;
		public string? IconName { get; set; }

		public string GamePath => Path.Combine(RootPath, "Game");
		public string DataPath => Path.Combine(RootPath, "Data");
	}
}
