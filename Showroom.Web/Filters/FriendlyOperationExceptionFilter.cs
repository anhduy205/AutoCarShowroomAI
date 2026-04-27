using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Showroom.Web.Models;
using Showroom.Web.Services;

namespace Showroom.Web.Filters;

public sealed class FriendlyOperationExceptionFilter : IExceptionFilter
{
    private readonly ILogger<FriendlyOperationExceptionFilter> _logger;
    private readonly IModelMetadataProvider _modelMetadataProvider;

    public FriendlyOperationExceptionFilter(
        ILogger<FriendlyOperationExceptionFilter> logger,
        IModelMetadataProvider modelMetadataProvider)
    {
        _logger = logger;
        _modelMetadataProvider = modelMetadataProvider;
    }

    public void OnException(ExceptionContext context)
    {
        if (context.Exception is not FriendlyOperationException ex)
        {
            return;
        }

        _logger.LogWarning(ex, "Friendly operation error on {Path}.", context.HttpContext.Request.Path);

        if (IsApiRequest(context.HttpContext))
        {
            context.Result = new ObjectResult(new ProblemDetails
            {
                Status = StatusCodes.Status400BadRequest,
                Title = "Invalid operation",
                Detail = ex.Message
            })
            {
                StatusCode = StatusCodes.Status400BadRequest
            };

            context.ExceptionHandled = true;
            return;
        }

        var viewData = new ViewDataDictionary(_modelMetadataProvider, context.ModelState)
        {
            Model = new FriendlyErrorViewModel
            {
                Title = "Khong the thuc hien yeu cau",
                Message = ex.Message,
                RequestId = context.HttpContext.TraceIdentifier
            }
        };

        context.Result = new ViewResult
        {
            ViewName = "~/Views/Shared/FriendlyError.cshtml",
            StatusCode = StatusCodes.Status400BadRequest,
            ViewData = viewData
        };

        context.ExceptionHandled = true;
    }

    private static bool IsApiRequest(HttpContext httpContext)
    {
        if (httpContext.Request.Path.StartsWithSegments("/api", StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        var accept = httpContext.Request.Headers.Accept.ToString();
        return accept.Contains("application/json", StringComparison.OrdinalIgnoreCase) ||
               accept.Contains("application/problem+json", StringComparison.OrdinalIgnoreCase);
    }
}
