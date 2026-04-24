using System.ComponentModel.DataAnnotations;
using Showroom.Web.Validation;

namespace Showroom.Web.Models;

public class AdminLoginViewModel
{
    [Required(ErrorMessage = "Vui long nhap ten dang nhap.")]
    [NotWhiteSpace(ErrorMessage = "Ten dang nhap khong duoc chi gom khoang trang.")]
    [Display(Name = "Ten dang nhap")]
    public string Username { get; set; } = string.Empty;

    [Required(ErrorMessage = "Vui long nhap mat khau.")]
    [NotWhiteSpace(ErrorMessage = "Mat khau khong duoc chi gom khoang trang.")]
    [DataType(DataType.Password)]
    [Display(Name = "Mat khau")]
    public string Password { get; set; } = string.Empty;

    public string? ReturnUrl { get; set; }
}
