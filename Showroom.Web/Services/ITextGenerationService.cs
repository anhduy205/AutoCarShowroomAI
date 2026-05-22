namespace Showroom.Web.Services;

public interface ITextGenerationService
{
    Task<AiChatResult> GenerateAsync(string prompt, CancellationToken cancellationToken = default);
}

