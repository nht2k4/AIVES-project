using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using AIVES.Business.Interfaces;
using AIVES.Business.Models;

namespace AIVES.Business.Services;

// TV2 - Ass3 - việc 2.3. Cổng gọi máy chủ VieNeu-TTS chạy cục bộ (API tương thích OpenAI). Xem AIVES_PhanCong_Ass2_Ass3.md.
// Lớp này gọi mạng nên KHÔNG có test tự động: kiểm bằng checklist trong tài liệu.
public class VieNeuTextToSpeech : ITextToSpeech
{
    private const int SampleRate = 48000;
    private static readonly HttpClient Http = new() { Timeout = TimeSpan.FromSeconds(30) };
    private readonly TtsOptions _options;

    public VieNeuTextToSpeech(TtsOptions options)
    {
        _options = options;
    }

    public async Task<TtsStatus> GetStatusAsync(CancellationToken ct = default)
    {
        using var deadline = CancellationTokenSource.CreateLinkedTokenSource(ct);
        deadline.CancelAfter(TimeSpan.FromSeconds(10));
        var health = await GetJsonAsync<HealthResponse>("/health", deadline.Token);
        return health is { Status: "ok" }
            ? new TtsStatus(true, health.Backend, health.SampleRate, health.MaxStreams, health.Active)
            : new TtsStatus(false, null, SampleRate, 0, 0);
    }

    public async Task<List<TtsVoiceInfo>?> GetVoicesAsync(CancellationToken ct = default)
    {
        var response = await GetJsonAsync<VoiceList>("/v1/voices", ct);
        return response?.Data?.Select(v => new TtsVoiceInfo(v.Name, v.Description ?? "", v.Gender, v.IsFeatured))
            .ToList();
    }

    public async Task<string?> EnrollVoiceAsync(string name, byte[] clip, string fileName, bool denoise,
        string? description, CancellationToken ct = default)
    {
        using var form = new MultipartFormDataContent();
        form.Add(new StringContent(name), "name");
        form.Add(new ByteArrayContent(clip), "file", fileName);
        form.Add(new StringContent(denoise ? "true" : "false"), "denoise");
        form.Add(new StringContent(description ?? ""), "description");

        try
        {
            using var response = await Http.PostAsync(Url("/v1/voices"), form, ct);
            if (response.IsSuccessStatusCode) return null;
            return response.StatusCode == HttpStatusCode.Conflict
                ? "Tên giọng đã được VieNeu-TTS sử dụng."
                : $"VieNeu-TTS không thể tạo giọng (HTTP {(int)response.StatusCode}).";
        }
        catch (Exception ex) when (ex is HttpRequestException or OperationCanceledException or IOException)
        {
            return "Không kết nối được với VieNeu-TTS.";
        }
    }

    public async Task<Stream?> OpenSpeechAsync(string text, string? voice, CancellationToken ct = default)
    {
        using var request = new HttpRequestMessage(HttpMethod.Post, Url("/v1/audio/speech"))
        {
            Content = JsonContent.Create(new
            {
                model = _options.Model,
                input = text,
                voice = string.IsNullOrWhiteSpace(voice) ? _options.Voice : voice,
                response_format = "pcm",
                stream_format = "audio",
                sample_rate = SampleRate
            })
        };
        HttpResponseMessage? response = null;
        try
        {
            response = await Http.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, ct);
            if (!response.IsSuccessStatusCode) return null;

            var body = await response.Content.ReadAsStreamAsync(ct);
            var stream = new SpeechStream(body, response);
            response = null;
            return stream;
        }
        catch (Exception ex) when (ex is HttpRequestException or OperationCanceledException or IOException)
        {
            return null;
        }
        finally
        {
            response?.Dispose();
        }
    }

    private async Task<T?> GetJsonAsync<T>(string path, CancellationToken ct) where T : class
    {
        try
        {
            using var response = await Http.GetAsync(Url(path), ct);
            return response.IsSuccessStatusCode ? await response.Content.ReadFromJsonAsync<T>(ct) : null;
        }
        catch (Exception ex) when (ex is HttpRequestException or OperationCanceledException or IOException or JsonException)
        {
            return null;
        }
    }

    private string Url(string path) => _options.BaseUrl.TrimEnd('/') + path;

    private sealed record HealthResponse(string Status, string? Backend,
        [property: JsonPropertyName("sample_rate")] int SampleRate,
        [property: JsonPropertyName("max_streams")] int MaxStreams, int Active);

    private sealed record VoiceList(List<VoiceInfo>? Data);
    private sealed record VoiceInfo(string Name, string? Description, string? Gender, JsonElement Featured)
    {
        public bool IsFeatured => Featured.ValueKind == JsonValueKind.True ||
            (Featured.ValueKind == JsonValueKind.Number && Featured.TryGetInt32(out var rank) && rank > 0);
    }

    private sealed class SpeechStream(Stream body, HttpResponseMessage response) : Stream
    {
        public override bool CanRead => body.CanRead;
        public override bool CanSeek => false;
        public override bool CanWrite => false;
        public override long Length => throw new NotSupportedException();
        public override long Position { get => throw new NotSupportedException(); set => throw new NotSupportedException(); }
        public override int Read(byte[] buffer, int offset, int count) => body.Read(buffer, offset, count);
        public override ValueTask<int> ReadAsync(Memory<byte> buffer, CancellationToken ct = default) => body.ReadAsync(buffer, ct);
        public override void Flush() => throw new NotSupportedException();
        public override long Seek(long offset, SeekOrigin origin) => throw new NotSupportedException();
        public override void SetLength(long value) => throw new NotSupportedException();
        public override void Write(byte[] buffer, int offset, int count) => throw new NotSupportedException();

        protected override void Dispose(bool disposing)
        {
            if (disposing) response.Dispose();
            base.Dispose(disposing);
        }
    }
}
