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

    [StringLength(30, ErrorMessage = "So dien thoai toi da 30 ky tu.")]
    [Display(Name = "So dien thoai")]
    public string? CustomerPhone { get; set; }

    [StringLength(254, ErrorMessage = "Email toi da 254 ky tu.")]
    [EmailAddress(ErrorMessage = "Email khong hop le.")]
    [Display(Name = "Email")]
    public string? CustomerEmail { get; set; }

    [StringLength(300, ErrorMessage = "Dia chi toi da 300 ky tu.")]
    [Display(Name = "Dia chi")]
    public string? CustomerAddress { get; set; }

    [StringLength(500, ErrorMessage = "Ghi chu toi da 500 ky tu.")]
    [Display(Name = "Ghi chu")]
    public string? Note { get; set; }

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
