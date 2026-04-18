namespace Showroom.Web.Services;

public class FriendlyOperationException : InvalidOperationException
{
    public FriendlyOperationException(string message, Exception? innerException = null)
        : base(message, innerException)
    {
    }
}
