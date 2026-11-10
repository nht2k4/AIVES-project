using System.ComponentModel;
using System.Diagnostics;
using AIVES.Business.Interfaces;
using AIVES.Business.Models;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Win32.SafeHandles;

namespace AIVES.Ass3.MVC.Infrastructure;

public class VieNeuLauncher : IHostedService
{
    private readonly TtsOptions _options;
    private readonly ITextToSpeech _tts;
    private readonly ILogger<VieNeuLauncher> _logger;
    private Process? _process;
    private SafeFileHandle? _job;
    private volatile bool _isShuttingDown;

    public VieNeuLauncher(TtsOptions options, ITextToSpeech tts, ILogger<VieNeuLauncher> logger)
    {
        _options = options;
        _tts = tts;
        _logger = logger;
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        if (!_options.AutoStart)
        {
            _logger.LogInformation("VieNeu-TTS AutoStart tắt.");
            return;
        }

        using var healthDeadline = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        healthDeadline.CancelAfter(TimeSpan.FromSeconds(2));
        var status = await _tts.GetStatusAsync(healthDeadline.Token);
        if (status.Online)
        {
            _logger.LogInformation("VieNeu-TTS đang chạy tại {Url}.", _options.BaseUrl);
            return;
        }
        cancellationToken.ThrowIfCancellationRequested();

        if (!Uri.TryCreate(_options.BaseUrl, UriKind.Absolute, out var baseUrl)
            || !baseUrl.IsLoopback || baseUrl.Scheme != Uri.UriSchemeHttp)
        {
            _logger.LogWarning("Tự khởi động VieNeu-TTS cần URL HTTP trên máy này.");
            return;
        }

        var dir = FindDirectory(_options.ServerPath);
        if (dir == null)
        {
            _logger.LogWarning("Không tìm thấy thư mục {Path} để khởi động VieNeu-TTS.", _options.ServerPath);
            return;
        }

        try
        {
            var psi = new ProcessStartInfo
            {
                FileName = "uv",
                Arguments = _options.UseGpu
                    ? "run --extra cuda python -m apps.openai_speech"
                    : "run python -m apps.openai_speech",
                WorkingDirectory = dir,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };
            psi.Environment["VIENEU_DEVICE"] = _options.UseGpu ? "cuda" : "cpu";
            psi.Environment["HOST"] = baseUrl.Host.Trim('[', ']');
            psi.Environment["PORT"] = baseUrl.Port.ToString(System.Globalization.CultureInfo.InvariantCulture);

            var process = new Process { StartInfo = psi };
            _process = process;
            string? lastError = null;
            process.ErrorDataReceived += (_, e) =>
            {
                if (!string.IsNullOrWhiteSpace(e.Data)) lastError = e.Data;
            };
            process.Exited += (_, _) =>
            {
                if (_isShuttingDown) return;
                process.WaitForExit();
                if (!_isShuttingDown)
                    _logger.LogWarning("VieNeu-TTS đã dừng (mã {ExitCode}). {LastError}", process.ExitCode, lastError);
            };

            process.Start();
            if (OperatingSystem.IsWindows())
            {
                try
                {
                    _job = ChildProcessJob.Attach(process);
                }
                catch (Win32Exception ex)
                {
                    _logger.LogWarning(ex, "Không gắn được VieNeu-TTS vào Windows Job Object.");
                }
            }
            process.BeginOutputReadLine();
            process.BeginErrorReadLine();
            process.EnableRaisingEvents = true;
            _logger.LogInformation("Đã khởi động VieNeu-TTS tại {Dir}.", dir);
        }
        catch (Exception ex) when (ex is Win32Exception or InvalidOperationException)
        {
            _logger.LogWarning(ex, "Không thể khởi động VieNeu-TTS bằng uv run. Hãy kiểm tra cài đặt uv.");
        }
    }

    public async Task StopAsync(CancellationToken cancellationToken)
    {
        _isShuttingDown = true;
        _job?.Dispose();
        var process = _process;
        if (process is null) return;

        try
        {
            if (!process.HasExited)
            {
                process.Kill(entireProcessTree: true);
                await process.WaitForExitAsync(cancellationToken);
            }
        }
        catch (Exception ex) when (ex is InvalidOperationException or Win32Exception or OperationCanceledException)
        {
            _logger.LogDebug(ex, "Không thể chờ VieNeu-TTS dừng.");
        }
        finally
        {
            process.Dispose();
        }
    }

    private static string? FindDirectory(string folderName)
    {
        var current = AppContext.BaseDirectory;
        while (!string.IsNullOrEmpty(current))
        {
            var target = Path.Combine(current, folderName);
            if (Directory.Exists(target)) return target;

            var parent = Directory.GetParent(current);
            if (parent == null) break;
            current = parent.FullName;
        }

        return null;
    }
}
