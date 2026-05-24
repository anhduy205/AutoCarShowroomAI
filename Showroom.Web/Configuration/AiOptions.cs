namespace Showroom.Web.Configuration;

public sealed class AiOptions
{
    public const string SectionName = "Ai";

    public string SystemPrompt { get; set; } =
        "Bạn là trợ lý cho showroom ô tô. Hãy trả lời ngắn gọn, thân thiện, và có ích.";

    public double Temperature { get; set; } = 0.2;

    public CloudflareWorkersAiOptions CloudflareWorkersAi { get; set; } = new();
}

public sealed class CloudflareWorkersAiOptions
{
    public string AccountId { get; set; } = string.Empty;

    public string ApiToken { get; set; } = string.Empty;

    public string BaseUrl { get; set; } = "https://api.cloudflare.com/client/v4";

    public string Model { get; set; } = "@cf/meta/llama-3.1-8b-instruct";

    public int TimeoutSeconds { get; set; } = 30;
}
