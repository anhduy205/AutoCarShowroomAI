using System.ComponentModel.DataAnnotations;
using Showroom.Web.Validation;

namespace Showroom.Web.Models;

public class AdminLoginViewModel
{
    [Required(ErrorMessage = "Vui lòng nhập tên đăng nhập.")]
    [NotWhiteSpace(ErrorMessage = "Tên đăng nhập không được chỉ gồm khoảng trắng.")]
    [Display(Name = "Tên đăng nhập")]
    public string Username { get; set; } = string.Empty;

    [Required(ErrorMessage = "Vui lòng nhập mật khẩu.")]
    [NotWhiteSpace(ErrorMessage = "Mật khẩu không được chỉ gồm khoảng trắng.")]
    [DataType(DataType.Password)]
    [Display(Name = "Mật khẩu")]
    public string Password { get; set; } = string.Empty;

    public string? ReturnUrl { get; set; }
}
