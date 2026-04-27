using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Options;
using Showroom.Web.Configuration;

namespace Showroom.Web.Services;

public sealed class OpenAiChatService : IAiChatService
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    private readonly HttpClient _httpClient;
    private readonly IOptionsMonitor<AiOptions> _options;
    private readonly ILogger<OpenAiChatService> _logger;

    public OpenAiChatService(
        HttpClient httpClient,
        IOptionsMonitor<AiOptions> options,
        ILogger<OpenAiChatService> logger)
    {
        _httpClient = httpClient;
        _options = options;
        _logger = logger;
    }

    public async Task<AiChatResult> GetReplyAsync(string userMessage, CancellationToken cancellationToken = default)
    {
        var options = _options.CurrentValue;
        var openAiOptions = options.OpenAi;

        if (string.IsNullOrWhiteSpace(openAiOptions.ApiKey))
        {
            throw new AiChatConfigurationException("AI chat chua duoc cau hinh (thieu API key).");
        }

        if (string.IsNullOrWhiteSpace(userMessage))
        {
            throw new FriendlyOperationException("Noi dung tin nhan khong duoc de trong.");
        }

        var baseUrl = (openAiOptions.BaseUrl ?? string.Empty).Trim();
        if (string.IsNullOrWhiteSpace(baseUrl))
        {
            baseUrl = "https://api.openai.com/v1";
        }

        var endpoint = $"{baseUrl.TrimEnd('/')}/chat/completions";

        var payload = new
        {
            model = string.IsNullOrWhiteSpace(openAiOptions.Model) ? "gpt-4o-mini" : openAiOptions.Model,
            temperature = options.Temperature,
            messages = BuildMessages(options.SystemPrompt, userMessage)
        };

        using var request = new HttpRequestMessage(HttpMethod.Post, endpoint);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", openAiOptions.ApiKey);
        request.Content = new StringContent(JsonSerializer.Serialize(payload, JsonOptions), Encoding.UTF8, "application/json");

        using var timeoutCts = CreateTimeoutCts(openAiOptions.TimeoutSeconds, cancellationToken);

        try
        {
            using var response = await _httpClient.SendAsync(
                request,
                HttpCompletionOption.ResponseHeadersRead,
                timeoutCts.Token);

            var raw = await response.Content.ReadAsStringAsync(timeoutCts.Token);

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning(
                    "OpenAI request failed with {StatusCode}. Body length={BodyLength}",
                    (int)response.StatusCode,
                    raw?.Length ?? 0);

                throw new AiChatUpstreamException("Khong the ket noi dich vu AI. Vui long thu lai sau.");
            }

            var text = TryParseChatCompletionContent(raw);
            if (string.IsNullOrWhiteSpace(text))
            {
                _logger.LogWarning("OpenAI returned empty content. Body length={BodyLength}", raw?.Length ?? 0);
                throw new AiChatUpstreamException("Khong nhan duoc phan hoi tu AI. Vui long thu lai sau.");
            }

            return new AiChatResult(text.Trim(), Provider: "OpenAi", Model: payload.model);
        }
        catch (FriendlyOperationException)
        {
            throw;
        }
        catch (OperationCanceledException ex) when (timeoutCts.IsCancellationRequested)
        {
            _logger.LogWarning(ex, "OpenAI request timed out.");
            throw new AiChatUpstreamException("He thong AI phan hoi qua lau. Vui long thu lai sau.", ex);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "OpenAI request failed.");
            throw new AiChatUpstreamException("Khong the ket noi dich vu AI. Vui long thu lai sau.", ex);
        }
    }

    private static object[] BuildMessages(string? systemPrompt, string userMessage)
    {
        if (!string.IsNullOrWhiteSpace(systemPrompt))
        {
            return new object[]
            {
                new { role = "system", content = systemPrompt },
                new { role = "user", content = userMessage.Trim() }
            };
        }

        return new object[]
        {
            new { role = "user", content = userMessage.Trim() }
        };
    }

    private static CancellationTokenSource CreateTimeoutCts(int timeoutSeconds, CancellationToken cancellationToken)
    {
        var timeout = TimeSpan.FromSeconds(Math.Clamp(timeoutSeconds, 5, 120));
        var timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        timeoutCts.CancelAfter(timeout);
        return timeoutCts;
    }

    private static string? TryParseChatCompletionContent(string? json)
    {
        if (string.IsNullOrWhiteSpace(json))
        {
            return null;
        }

        using var doc = JsonDocument.Parse(json);

        if (!doc.RootElement.TryGetProperty("choices", out var choices) ||
            choices.ValueKind != JsonValueKind.Array ||
            choices.GetArrayLength() == 0)
        {
            return null;
        }

        var choice = choices[0];
        if (!choice.TryGetProperty("message", out var message) ||
            message.ValueKind != JsonValueKind.Object)
        {
            return null;
        }

        if (!message.TryGetProperty("content", out var content))
        {
            return null;
        }

        return content.ValueKind switch
        {
            JsonValueKind.String => content.GetString(),
            _ => content.ToString()
        };
    }
}
