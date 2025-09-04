namespace CMLauncher
{
	internal class InstallationInfoFile
	{
		// Required fields
		public string? version { get; set; } // human-friendly name e.g. 1.9.8b3-beta
		public string? manifest { get; set; } // numeric manifest id (string)

		// Keep other metadata
		public string? timestamp { get; set; }
		public string? icon { get; set; }
	}
}
