using System.ComponentModel.DataAnnotations;
using Showroom.Web.Models;

namespace Showroom.Web.Tests;

public class OrderValidationTests
{
    [Fact]
    public void RejectsWhitespaceCustomerName()
    {
        var model = new OrderFormViewModel
        {
            CustomerName = "   ",
            Status = OrderStatusCatalog.Pending
        };

        var results = Validate(model);

        Assert.Contains(results, result => result.MemberNames.Contains(nameof(OrderFormViewModel.CustomerName)));
    }

    [Fact]
    public void RejectsUnknownStatus()
    {
        var model = new OrderFormViewModel
        {
            CustomerName = "Nguyen Van A",
            Status = "Archived"
        };

        var results = Validate(model);

        Assert.Contains(results, result => result.MemberNames.Contains(nameof(OrderFormViewModel.Status)));
    }

    private static IReadOnlyList<ValidationResult> Validate(OrderFormViewModel model)
    {
        var validationContext = new ValidationContext(model);
        var results = new List<ValidationResult>();
        Validator.TryValidateObject(model, validationContext, results, validateAllProperties: true);
        return results;
    }
}
