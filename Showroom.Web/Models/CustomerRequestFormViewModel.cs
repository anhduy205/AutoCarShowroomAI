using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.Rendering;
using Showroom.Web.Validation;

namespace Showroom.Web.Models;

public sealed class CustomerRequestFormViewModel : IValidatableObject
{
    [Required(ErrorMessage = "Vui long chon nhu cau.")]
    [Display(Name = "Nhu cau")]
    public string RequestType { get; set; } = CustomerRequestCatalog.ViewCar;

    [Display(Name = "Xe quan tam")]
    public int? CarId { get; set; }

    [Required(ErrorMessage = "Vui long nhap ten khach hang.")]
    [NotWhiteSpace(ErrorMessage = "Ten khach hang khong duoc chi gom khoang trang.")]
    [StringLength(150, ErrorMessage = "Ten khach hang toi da 150 ky tu.")]
    [Display(Name = "Ho ten")]
    public string CustomerName { get; set; } = string.Empty;

    [StringLength(30, ErrorMessage = "So dien thoai toi da 30 ky tu.")]
    [Display(Name = "So dien thoai")]
    public string? CustomerPhone { get; set; }

    [StringLength(254, ErrorMessage = "Email toi da 254 ky tu.")]
    [EmailAddress(ErrorMessage = "Email khong hop le.")]
    [Display(Name = "Email")]
    public string? CustomerEmail { get; set; }

    [Display(Name = "Thoi gian mong muon")]
    public DateTime? PreferredTime { get; set; }

    [Range(0, 999999999999, ErrorMessage = "So tien dat coc khong hop le.")]
    [Display(Name = "So tien dat coc du kien")]
    public decimal? DepositAmount { get; set; }

    [StringLength(500, ErrorMessage = "Ghi chu toi da 500 ky tu.")]
    [Display(Name = "Ghi chu")]
    public string? Note { get; set; }

    public IReadOnlyList<SelectListItem> RequestTypeOptions { get; set; } = CustomerRequestCatalog.GetTypeSelectList();

    public IReadOnlyList<SelectListItem> CarOptions { get; set; } = Array.Empty<SelectListItem>();

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (!CustomerRequestCatalog.IsValidType(RequestType))
        {
            yield return new ValidationResult("Nhu cau khong hop le.", new[] { nameof(RequestType) });
        }

        if (string.IsNullOrWhiteSpace(CustomerPhone) && string.IsNullOrWhiteSpace(CustomerEmail))
        {
            yield return new ValidationResult(
                "Vui long nhap so dien thoai hoac email de showroom thong bao khi xac nhan.",
                new[] { nameof(CustomerPhone), nameof(CustomerEmail) });
        }

        if (RequestType == CustomerRequestCatalog.Deposit && (DepositAmount is null or <= 0))
        {
            yield return new ValidationResult(
                "Vui long nhap so tien dat coc du kien.",
                new[] { nameof(DepositAmount) });
        }
    }
}
