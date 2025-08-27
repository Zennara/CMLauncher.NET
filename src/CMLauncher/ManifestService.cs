using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace CMLauncher
{
	public class ManifestEntry
	{
		[JsonPropertyName("version")] public string? Version { get; set; }
		[JsonPropertyName("manifest_id")] public string ManifestId { get; set; } = string.Empty;
		[JsonPropertyName("branch")] public string? Branch { get; set; }
		[JsonPropertyName("timestamp")] public string? Timestamp { get; set; }
	}

	public static class ManifestService
	{
		private static readonly HttpClient _http = new HttpClient();
		private const string CmzUrl = "https://raw.githubusercontent.com/Zennara/CMLauncher.NET/refs/heads/main/data/cmz-manifests.json";

		public static async Task<List<ManifestEntry>> FetchCmzManifestsAsync()
		{
			var list = new List<ManifestEntry>();
			try
			{
				using var resp = await _http.GetAsync(CmzUrl).ConfigureAwait(false);
				resp.EnsureSuccessStatusCode();
				await using var stream = await resp.Content.ReadAsStreamAsync().ConfigureAwait(false);
				var data = await JsonSerializer.DeserializeAsync<List<ManifestEntry>>(stream, new JsonSerializerOptions
				{
					PropertyNameCaseInsensitive = true
				}).ConfigureAwait(false);
				if (data != null)
				{
					list = data;
				}
			}
			catch
			{
				// ignore network/parse errors, return empty
			}
			return list;
		}
	}
}
