using System.Diagnostics;

public class DepotDownloaderWrapper
{
	private readonly string _exePath;
	private Process? _process;

	// Single event for both stdout and stderr
	public event Action<string>? OutputReceived;

	public DepotDownloaderWrapper(string exePath)
	{
		_exePath = exePath;
	}

	public async Task<int> RunAsync(string arguments)
	{
		var startInfo = new ProcessStartInfo
		{
			FileName = _exePath,
			Arguments = arguments,
			RedirectStandardInput = true,
			RedirectStandardOutput = true,
			RedirectStandardError = true,
			UseShellExecute = false,
			CreateNoWindow = true
		};

		_process = new Process { StartInfo = startInfo, EnableRaisingEvents = true };

		_process.OutputDataReceived += (s, e) =>
		{
			if (!string.IsNullOrEmpty(e.Data))
				OutputReceived?.Invoke(e.Data);
		};

		_process.ErrorDataReceived += (s, e) =>
		{
			if (!string.IsNullOrEmpty(e.Data))
				OutputReceived?.Invoke(e.Data); // send error lines to same OutputReceived
		};

		_process.Start();
		_process.BeginOutputReadLine();
		_process.BeginErrorReadLine();

		await _process.WaitForExitAsync();
		return _process.ExitCode;
	}

	public void SendInput(string input)
	{
		if (_process != null && !_process.HasExited)
		{
			_process.StandardInput.WriteLine(input);
		}
	}
}
