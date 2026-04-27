namespace Showroom.Web.Models;

public sealed class FriendlyErrorViewModel
{
    public string Title { get; set; } = "Da xay ra loi";

    public string Message { get; set; } = string.Empty;

    public string? RequestId { get; set; }
}

