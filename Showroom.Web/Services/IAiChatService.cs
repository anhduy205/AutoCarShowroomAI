namespace Showroom.Web.Services;

public interface IAiChatService
{
    Task<AiChatResult> GetReplyAsync(string userMessage, CancellationToken cancellationToken = default);
}

public sealed record AiChatResult(string Text, string Provider, string? Model = null);

