using System.Diagnostics;
using AIVES.Business.Models;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace AIVES.Ass3.MVC.Infrastructure;

public class VieNeuLauncher : IHostedService
{
    private readonly TtsOptions _options;
    private readonly ILogger<VieNeuLauncher> _logger;
    private Process? _process;
    private bool _isShuttingDown;

    public VieNeuLauncher(TtsOptions options, ILogger<VieNeuLauncher> logger)
    {
        _options = options;
        _logger = logger;
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        if (!_options.AutoStart)
        {
            _logger.LogInformation("VieNeu-TTS AutoStart tắt.");
            return;
        }

        try
        {
            using var http = new HttpClient { Timeout = TimeSpan.FromSeconds(2) };
            var health = await http.GetAsync($"{_options.BaseUrl.TrimEnd('/')}/health", cancellationToken);
            if (health.IsSuccessStatusCode)
            {
                _logger.LogInformation("VieNeu-TTS đang chạy tại {Url}.", _options.BaseUrl);
                return;
            }
        }
        catch
        {
            // Chưa chạy, tiến hành khởi động
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
                Arguments = "run python -m apps.openai_speech",
                WorkingDirectory = dir,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };

            _process = new Process { StartInfo = psi, EnableRaisingEvents = true };
            _process.Exited += (s, e) =>
            {
                if (!_isShuttingDown)
                {
                    _logger.LogWarning("VieNeu-TTS đã dừng (mã {ExitCode}).", _process?.ExitCode);
                }
            };

            _process.Start();
            _process.BeginOutputReadLine();
            _process.BeginErrorReadLine();
            _logger.LogInformation("Đã khởi động VieNeu-TTS tại {Dir}.", dir);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Không thể khởi động VieNeu-TTS bằng uv run. Hãy kiểm tra cài đặt uv.");
        }
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        _isShuttingDown = true;
        try
        {
            if (_process != null && !_process.HasExited)
            {
                _process.Kill(true);
            }
        }
        catch
        {
            // Bỏ qua lỗi khi tắt process
        }

        return Task.CompletedTask;
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
