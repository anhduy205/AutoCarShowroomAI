using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.Rendering;
using Showroom.Web.Validation;

namespace Showroom.Web.Models;

public class OrderFormViewModel : IValidatableObject
{
    public int Id { get; set; }

    [Required(ErrorMessage = "Vui long nhap ten khach hang.")]
    [NotWhiteSpace(ErrorMessage = "Ten khach hang khong duoc chi gom khoang trang.")]
    [StringLength(150, ErrorMessage = "Ten khach hang toi da 150 ky tu.")]
    [Display(Name = "Khach hang")]
    public string CustomerName { get; set; } = string.Empty;

    [Required(ErrorMessage = "Vui long chon trang thai don hang.")]
    [Display(Name = "Trang thai")]
    public string Status { get; set; } = OrderStatusCatalog.Pending;

    public List<OrderFormItemViewModel> Items { get; set; } = new()
    {
        new OrderFormItemViewModel()
    };

    public IReadOnlyList<SelectListItem> CarOptions { get; set; } = Array.Empty<SelectListItem>();

    public IReadOnlyList<SelectListItem> StatusOptions { get; set; } = OrderStatusCatalog.GetSelectList();

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (!OrderStatusCatalog.IsValid(Status))
        {
            yield return new ValidationResult(
                "Trang thai don hang khong hop le.",
                new[] { nameof(Status) });
        }
    }
}
