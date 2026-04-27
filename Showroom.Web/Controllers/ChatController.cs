using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Showroom.Web.Security;
using Showroom.Web.Models;
using Showroom.Web.Services;

namespace Showroom.Web.Controllers;

[ApiController]
[Route("api/chat")]
[EnableRateLimiting("ChatApi")]
public sealed class ChatController : ControllerBase
{
    private readonly IAiChatService _aiChatService;
    private readonly IAuditLogService _auditLogService;
    private readonly ILogger<ChatController> _logger;

    public ChatController(IAiChatService aiChatService, IAuditLogService auditLogService, ILogger<ChatController> logger)
    {
        _aiChatService = aiChatService;
        _auditLogService = auditLogService;
        _logger = logger;
    }

    [HttpPost]
    [Produces("application/json")]
    [ProducesResponseType(typeof(ChatResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status503ServiceUnavailable)]
    public async Task<ActionResult<ChatResponse>> Post([FromBody] ChatRequest request, CancellationToken cancellationToken)
    {
        var message = request.Message?.Trim() ?? string.Empty;
        if (string.IsNullOrWhiteSpace(message))
        {
            return BadRequest(new ProblemDetails
            {
                Status = StatusCodes.Status400BadRequest,
                Title = "Invalid request",
                Detail = "Noi dung tin nhan khong duoc de trong."
            });
        }

        if (message.Length > 1000)
        {
            return BadRequest(new ProblemDetails
            {
                Status = StatusCodes.Status400BadRequest,
                Title = "Invalid request",
                Detail = "Noi dung tin nhan qua dai (toi da 1000 ky tu)."
            });
        }

        try
        {
            var ip = HttpContext.Connection.RemoteIpAddress?.ToString() ?? string.Empty;
            var preview = InputSanitizer.SanitizePreview(message, 180);
            var stopwatch = System.Diagnostics.Stopwatch.StartNew();

            var result = await _aiChatService.GetReplyAsync(message, cancellationToken);

            stopwatch.Stop();
            await TryWriteChatAuditAsync(
                action: "CHAT_REQUEST",
                description: $"TraceId={HttpContext.TraceIdentifier}; Ip={ip}; Ms={stopwatch.ElapsedMilliseconds}; Message='{preview}'",
                ipAddress: ip,
                cancellationToken: cancellationToken);

            return Ok(new ChatResponse
            {
                Reply = result.Text,
                Provider = result.Provider,
                Model = result.Model,
                TraceId = HttpContext.TraceIdentifier
            });
        }
        catch (AiChatConfigurationException ex)
        {
            _logger.LogWarning(ex, "Chat is not configured.");
            await TryWriteChatAuditAsync(
                action: "CHAT_FAILED",
                description: $"TraceId={HttpContext.TraceIdentifier}; Reason=Configuration",
                ipAddress: HttpContext.Connection.RemoteIpAddress?.ToString() ?? string.Empty,
                cancellationToken: cancellationToken);
            return StatusCode(
                StatusCodes.Status503ServiceUnavailable,
                new ProblemDetails
                {
                    Status = StatusCodes.Status503ServiceUnavailable,
                    Title = "Chatbot unavailable",
                    Detail = ex.Message
                });
        }
        catch (AiChatUpstreamException ex)
        {
            _logger.LogWarning(ex, "Chat upstream failed.");
            await TryWriteChatAuditAsync(
                action: "CHAT_FAILED",
                description: $"TraceId={HttpContext.TraceIdentifier}; Reason=Upstream",
                ipAddress: HttpContext.Connection.RemoteIpAddress?.ToString() ?? string.Empty,
                cancellationToken: cancellationToken);
            return StatusCode(
                StatusCodes.Status503ServiceUnavailable,
                new ProblemDetails
                {
                    Status = StatusCodes.Status503ServiceUnavailable,
                    Title = "Chatbot unavailable",
                    Detail = ex.Message
                });
        }
    }

    private async Task TryWriteChatAuditAsync(string action, string description, string ipAddress, CancellationToken cancellationToken)
    {
        try
        {
            await _auditLogService.WriteAsync(
                new AuditLogEntry
                {
                    Username = "Public",
                    DisplayName = "Public",
                    Role = "Public",
                    Action = action,
                    EntityType = "Chat",
                    Description = description,
                    IpAddress = ipAddress
                },
                cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogDebug(ex, "Skip writing chat audit log.");
        }
    }

    public sealed class ChatRequest
    {
        [Required]
        [MaxLength(1000)]
        public string Message { get; set; } = string.Empty;
    }

    public sealed class ChatResponse
    {
        public string Reply { get; set; } = string.Empty;

        public string Provider { get; set; } = string.Empty;

        public string? Model { get; set; }

        public string TraceId { get; set; } = string.Empty;
    }
}
