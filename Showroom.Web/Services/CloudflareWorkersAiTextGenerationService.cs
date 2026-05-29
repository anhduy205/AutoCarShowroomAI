using System.Net;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Options;
using Showroom.Web.Configuration;

namespace Showroom.Web.Services;

public sealed class CloudflareWorkersAiTextGenerationService : ITextGenerationService
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    private readonly HttpClient _httpClient;
    private readonly IOptionsMonitor<AiOptions> _options;
    private readonly ILogger<CloudflareWorkersAiTextGenerationService> _logger;

    public CloudflareWorkersAiTextGenerationService(
        HttpClient httpClient,
        IOptionsMonitor<AiOptions> options,
        ILogger<CloudflareWorkersAiTextGenerationService> logger)
    {
        _httpClient = httpClient;
        _options = options;
        _logger = logger;
    }

    public async Task<AiChatResult> GenerateAsync(string prompt, CancellationToken cancellationToken = default)
    {
        var options = _options.CurrentValue;
        var workersAi = options.CloudflareWorkersAi;

        if (string.IsNullOrWhiteSpace(workersAi.AccountId) ||
            string.IsNullOrWhiteSpace(workersAi.ApiToken))
        {
            throw new AiChatConfigurationException("AI chat chưa được cấu hình (thiếu Cloudflare AccountId/ApiToken).");
        }

        if (LooksLikeApiToken(workersAi.AccountId))
        {
            throw new AiChatConfigurationException(
                "Cloudflare AccountId không hợp lệ (có vẻ đang là API token). Hãy copy Account ID (32 ký tự hex) từ Cloudflare Dashboard.");
        }

        if (string.IsNullOrWhiteSpace(prompt))
        {
            throw new FriendlyOperationException("Nội dung tin nhắn không được để trống.");
        }

        var baseUrl = (workersAi.BaseUrl ?? string.Empty).Trim();
        if (string.IsNullOrWhiteSpace(baseUrl))
        {
            baseUrl = "https://api.cloudflare.com/client/v4";
        }

        var model = string.IsNullOrWhiteSpace(workersAi.Model)
            ? "@cf/meta/llama-3.1-8b-instruct"
            : workersAi.Model.Trim();

        // Model names contain '/' (e.g. @cf/meta/llama-3.1-8b-instruct). The Cloudflare API expects these as path segments.
        // Ensure we don't accidentally double-slash when the model is configured with a leading '/'.
        model = model.TrimStart('/');
        var endpoint = $"{baseUrl.TrimEnd('/')}/accounts/{workersAi.AccountId.Trim()}/ai/run/{model}";

        var requestBody = BuildRequestBody(options.SystemPrompt, prompt);

        using var request = new HttpRequestMessage(HttpMethod.Post, endpoint);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", workersAi.ApiToken.Trim());
        request.Content = new StringContent(JsonSerializer.Serialize(requestBody, JsonOptions), Encoding.UTF8, "application/json");

        using var timeoutCts = CreateTimeoutCts(workersAi.TimeoutSeconds, cancellationToken);

        try
        {
            using var response = await _httpClient.SendAsync(
                request,
                HttpCompletionOption.ResponseHeadersRead,
                timeoutCts.Token);

            var raw = await response.Content.ReadAsStringAsync(timeoutCts.Token);
            if (!response.IsSuccessStatusCode)
            {
                var upstream = TryParseUpstreamError(raw);
                _logger.LogWarning(
                    "Workers AI request failed with {StatusCode}. Code={ErrorCode}; Message={ErrorMessage}; Body length={BodyLength}",
                    (int)response.StatusCode,
                    upstream.Code ?? "-",
                    upstream.Message ?? "-",
                    raw?.Length ?? 0);

                throw new AiChatUpstreamException(BuildFriendlyUpstreamMessage(response.StatusCode, upstream, model));
            }

            var result = TryParseText(raw);
            if (string.IsNullOrWhiteSpace(result))
            {
                _logger.LogWarning("Workers AI returned empty content. Body length={BodyLength}", raw?.Length ?? 0);
                throw new AiChatUpstreamException("Không nhận được phản hồi từ AI. Vui lòng thử lại sau.");
            }

            return new AiChatResult(result.Trim(), Provider: "CloudflareWorkersAi", Model: model);
        }
        catch (FriendlyOperationException)
        {
            throw;
        }
        catch (HttpRequestException ex)
        {
            _logger.LogWarning(ex, "Workers AI request failed due to network error.");
            throw new AiChatUpstreamException("Không thể kết nối tới Cloudflare Workers AI (mạng/DNS/proxy).", ex);
        }
        catch (OperationCanceledException ex) when (timeoutCts.IsCancellationRequested)
        {
            _logger.LogWarning(ex, "Workers AI request timed out.");
            throw new AiChatUpstreamException("Hệ thống AI phản hồi quá lâu. Vui lòng thử lại sau.", ex);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Workers AI request failed.");
            throw new AiChatUpstreamException("Không thể kết nối dịch vụ AI. Vui lòng thử lại sau.", ex);
        }
    }

    private static object BuildRequestBody(string? systemPrompt, string userPrompt)
    {
        if (!string.IsNullOrWhiteSpace(systemPrompt))
        {
            return new
            {
                messages = new[]
                {
                    new { role = "system", content = systemPrompt.Trim() },
                    new { role = "user", content = userPrompt.Trim() }
                }
            };
        }

        return new
        {
            prompt = userPrompt.Trim()
        };
    }

    private static CancellationTokenSource CreateTimeoutCts(int timeoutSeconds, CancellationToken cancellationToken)
    {
        var timeout = TimeSpan.FromSeconds(Math.Clamp(timeoutSeconds, 5, 120));
        var timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        timeoutCts.CancelAfter(timeout);
        return timeoutCts;
    }

    private static string? TryParseText(string? json)
    {
        if (string.IsNullOrWhiteSpace(json))
        {
            return null;
        }

        using var doc = JsonDocument.Parse(json);
        if (!doc.RootElement.TryGetProperty("result", out var result))
        {
            return null;
        }

        // Text generation typically returns { "result": { "response": "..." } }
        if (result.ValueKind == JsonValueKind.Object &&
            result.TryGetProperty("response", out var response))
        {
            return response.ValueKind == JsonValueKind.String ? response.GetString() : response.ToString();
        }

        return null;
    }

    private static bool LooksLikeApiToken(string value)
    {
        var trimmed = value.Trim();
        return trimmed.StartsWith("cfut_", StringComparison.OrdinalIgnoreCase) ||
               trimmed.StartsWith("cfat_", StringComparison.OrdinalIgnoreCase) ||
               trimmed.Contains('_', StringComparison.Ordinal);
    }

    private static UpstreamError TryParseUpstreamError(string? json)
    {
        if (string.IsNullOrWhiteSpace(json))
        {
            return new UpstreamError(null, null);
        }

        try
        {
            using var doc = JsonDocument.Parse(json);
            if (doc.RootElement.TryGetProperty("errors", out var errors) &&
                errors.ValueKind == JsonValueKind.Array &&
                errors.GetArrayLength() > 0)
            {
                var first = errors[0];
                var code = first.TryGetProperty("code", out var codeElement) ? codeElement.ToString() : null;
                var message = first.TryGetProperty("message", out var messageElement) && messageElement.ValueKind == JsonValueKind.String
                    ? messageElement.GetString()
                    : null;

                return new UpstreamError(code, message);
            }

            return new UpstreamError(null, null);
        }
        catch
        {
            return new UpstreamError(null, null);
        }
    }

    private static string BuildFriendlyUpstreamMessage(HttpStatusCode statusCode, UpstreamError error, string model)
        => statusCode switch
        {
            HttpStatusCode.Unauthorized or HttpStatusCode.Forbidden =>
                "Cloudflare Workers AI tu choi xac thuc (401/403). Kiểm tra API token va Account ID.",
            (HttpStatusCode)429 =>
                "Cloudflare Workers AI giới hạn yêu cầu (429) hoặc hết quota. Hãy thử lại sau.",
            HttpStatusCode.NotFound =>
                string.IsNullOrWhiteSpace(error.Message)
                    ? $"Không tìm thấy model/endpoint (404). Kiểm tra Ai:CloudflareWorkersAi:Model (hiện tại: {model}) và Ai:CloudflareWorkersAi:BaseUrl."
                    : $"Không tìm thấy model/endpoint (404). {error.Message}",
            HttpStatusCode.BadRequest =>
                string.IsNullOrWhiteSpace(error.Message)
                    ? "Yêu cầu tới Cloudflare Workers AI không hợp lệ (400). Kiểm tra cấu hình model/prompt."
                    : $"Yêu cầu tới Cloudflare Workers AI không hợp lệ (400). {error.Message}",
            _ =>
                $"Cloudflare Workers AI trả về lỗi HTTP {(int)statusCode}. Vui lòng thử lại sau."
        };

    private sealed record UpstreamError(string? Code, string? Message);
}
