using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.Rendering;
using Showroom.Web.Validation;

namespace Showroom.Web.Models;

public sealed class CustomerFormViewModel
{
    public int Id { get; set; }

    [Required(ErrorMessage = "Vui lòng nhập mã khách hàng.")]
    [NotWhiteSpace(ErrorMessage = "Mã khách hàng không được chỉ gồm khoảng trắng.")]
    [StringLength(30, ErrorMessage = "Mã khách hàng tối đa 30 ký tự.")]
    [Display(Name = "Mã khách hàng")]
    public string CustomerCode { get; set; } = string.Empty;

    [Required(ErrorMessage = "Vui lòng nhập tên khách hàng.")]
    [NotWhiteSpace(ErrorMessage = "Tên khách hàng không được chỉ gồm khoảng trắng.")]
    [StringLength(150, ErrorMessage = "Tên khách hàng tối đa 150 ký tự.")]
    [Display(Name = "Tên khách hàng")]
    public string FullName { get; set; } = string.Empty;

    [StringLength(30, ErrorMessage = "Số điện thoại tối đa 30 ký tự.")]
    [Display(Name = "Số điện thoại")]
    public string? Phone { get; set; }

    [EmailAddress(ErrorMessage = "Email không hợp lệ.")]
    [StringLength(254, ErrorMessage = "Email tối đa 254 ký tự.")]
    [Display(Name = "Email")]
    public string? Email { get; set; }

    [StringLength(50, ErrorMessage = "Mã số thuế tối đa 50 ký tự.")]
    [Display(Name = "Mã số thuế")]
    public string? TaxCode { get; set; }

    [StringLength(300, ErrorMessage = "Địa chỉ tối đa 300 ký tự.")]
    [Display(Name = "Địa chỉ")]
    public string? Address { get; set; }

    [Required(ErrorMessage = "Vui lòng chọn loại khách hàng.")]
    [Display(Name = "Loại khách hàng")]
    public string CustomerType { get; set; } = CustomerTypeCatalog.Individual;

    public IReadOnlyList<SelectListItem> CustomerTypeOptions { get; set; } =
        CustomerTypeCatalog.GetSelectList(CustomerTypeCatalog.Individual);
}
