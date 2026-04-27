namespace Showroom.Web.Configuration;

public sealed class AiOptions
{
    public const string SectionName = "Ai";

    public string Provider { get; set; } = "OpenAi";

    public string SystemPrompt { get; set; } =
        "Ban la tro ly cho showroom o to. Hay tra loi ngan gon, than thien, va co ich.";

    public double Temperature { get; set; } = 0.2;

    public OpenAiOptions OpenAi { get; set; } = new();
}

public sealed class OpenAiOptions
{
    public string ApiKey { get; set; } = string.Empty;

    public string BaseUrl { get; set; } = "https://api.openai.com/v1";

    public string Model { get; set; } = "gpt-4o-mini";

    public int TimeoutSeconds { get; set; } = 30;
}

