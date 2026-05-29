using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.Rendering;
using Showroom.Web.Validation;

namespace Showroom.Web.Models;

public sealed class CustomerRequestFormViewModel : IValidatableObject
{
    [Required(ErrorMessage = "Vui lòng chọn nhu cầu.")]
    [Display(Name = "Nhu cầu")]
    public string RequestType { get; set; } = CustomerRequestCatalog.ViewCar;

    [Display(Name = "Xe quan tâm")]
    public int? CarId { get; set; }

    [Required(ErrorMessage = "Vui lòng nhập tên khách hàng.")]
    [NotWhiteSpace(ErrorMessage = "Tên khách hàng không được chỉ gồm khoảng trắng.")]
    [StringLength(150, ErrorMessage = "Tên khách hàng tối đa 150 ký tự.")]
    [Display(Name = "Họ tên")]
    public string CustomerName { get; set; } = string.Empty;

    [StringLength(30, ErrorMessage = "Số điện thoại tối đa 30 ký tự.")]
    [Display(Name = "Số điện thoại")]
    public string? CustomerPhone { get; set; }

    [StringLength(254, ErrorMessage = "Email tối đa 254 ký tự.")]
    [EmailAddress(ErrorMessage = "Email không hợp lệ.")]
    [Display(Name = "Email")]
    public string? CustomerEmail { get; set; }

    [Display(Name = "Thời gian mong muốn")]
    public DateTime? PreferredTime { get; set; }

    [Range(0, 999999999999, ErrorMessage = "Số tiền đặt cọc không hợp lệ.")]
    [Display(Name = "Số tiền đặt cọc dự kiến")]
    public decimal? DepositAmount { get; set; }

    [StringLength(500, ErrorMessage = "Ghi chú tối đa 500 ký tự.")]
    [Display(Name = "Ghi chú")]
    public string? Note { get; set; }

    public IReadOnlyList<SelectListItem> RequestTypeOptions { get; set; } = CustomerRequestCatalog.GetTypeSelectList();

    public IReadOnlyList<SelectListItem> CarOptions { get; set; } = Array.Empty<SelectListItem>();

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (!CustomerRequestCatalog.IsValidType(RequestType))
        {
            yield return new ValidationResult("Nhu cầu không hợp lệ.", new[] { nameof(RequestType) });
        }

        if (string.IsNullOrWhiteSpace(CustomerPhone) && string.IsNullOrWhiteSpace(CustomerEmail))
        {
            yield return new ValidationResult(
                "Vui lòng nhập số điện thoại hoặc email để showroom thông báo khi xác nhận.",
                new[] { nameof(CustomerPhone), nameof(CustomerEmail) });
        }

        if (RequestType == CustomerRequestCatalog.Deposit && (DepositAmount is null or <= 0))
        {
            yield return new ValidationResult(
                "Vui lòng nhập số tiền đặt cọc dự kiến.",
                new[] { nameof(DepositAmount) });
        }
    }
}
