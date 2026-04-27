namespace Showroom.Web.Services;

public sealed class AiChatConfigurationException : FriendlyOperationException
{
    public AiChatConfigurationException(string message, Exception? innerException = null)
        : base(message, innerException)
    {
    }
}

public sealed class AiChatUpstreamException : FriendlyOperationException
{
    public AiChatUpstreamException(string message, Exception? innerException = null)
        : base(message, innerException)
    {
    }
}

