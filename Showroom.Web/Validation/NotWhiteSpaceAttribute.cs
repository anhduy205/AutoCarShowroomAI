using System.ComponentModel.DataAnnotations;

namespace Showroom.Web.Validation;

public sealed class NotWhiteSpaceAttribute : ValidationAttribute
{
    protected override ValidationResult? IsValid(object? value, ValidationContext validationContext)
    {
        if (value is null)
        {
            return ValidationResult.Success;
        }

        if (value is string text && string.IsNullOrWhiteSpace(text))
        {
            return new ValidationResult(ErrorMessage ?? $"{validationContext.DisplayName} is required.");
        }

        return ValidationResult.Success;
    }
}
