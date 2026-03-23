using System.ComponentModel.DataAnnotations;

namespace Showroom.Web.Models;

public class AdminLoginViewModel
{
    [Required]
    [Display(Name = "Tên đăng nhập")]
    public string Username { get; set; } = string.Empty;

    [Required]
    [DataType(DataType.Password)]
    [Display(Name = "Mật khẩu")]
    public string Password { get; set; } = string.Empty;

    public string? ReturnUrl { get; set; }
}
