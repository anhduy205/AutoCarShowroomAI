using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.Rendering;
using Showroom.Web.Validation;

namespace Showroom.Web.Models;

public class OrderFormViewModel : IValidatableObject
{
    public int Id { get; set; }

    [Required(ErrorMessage = "Vui lòng nhập tên khách hàng.")]
    [NotWhiteSpace(ErrorMessage = "Tên khách hàng không được chỉ gồm khoảng trắng.")]
    [StringLength(150, ErrorMessage = "Tên khách hàng tối đa 150 ký tự.")]
    [Display(Name = "Khách hàng")]
    public string CustomerName { get; set; } = string.Empty;

    [StringLength(30, ErrorMessage = "Số điện thoại tối đa 30 ký tự.")]
    [Display(Name = "Số điện thoại")]
    public string? CustomerPhone { get; set; }

    [StringLength(254, ErrorMessage = "Email tối đa 254 ký tự.")]
    [EmailAddress(ErrorMessage = "Email không hợp lệ.")]
    [Display(Name = "Email")]
    public string? CustomerEmail { get; set; }

    [StringLength(300, ErrorMessage = "Địa chỉ tối đa 300 ký tự.")]
    [Display(Name = "Địa chỉ")]
    public string? CustomerAddress { get; set; }

    [StringLength(500, ErrorMessage = "Ghi chú tối đa 500 ký tự.")]
    [Display(Name = "Ghi chú")]
    public string? Note { get; set; }

    [Required(ErrorMessage = "Vui lòng chọn trạng thái đơn hàng.")]
    [Display(Name = "Trạng thái")]
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
                "Trạng thái đơn hàng không hợp lệ.",
                new[] { nameof(Status) });
        }
    }
}
