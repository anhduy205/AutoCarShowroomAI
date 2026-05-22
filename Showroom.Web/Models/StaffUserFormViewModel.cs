using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.Rendering;
using Showroom.Web.Security;

namespace Showroom.Web.Models;

public sealed class StaffUserFormViewModel
{
    public int Id { get; set; }

    [Required(ErrorMessage = "Tên đăng nhập không được để trống.")]
    [MaxLength(100)]
    public string Username { get; set; } = string.Empty;

    [Required(ErrorMessage = "Tên hiển thị không được để trống.")]
    [MaxLength(150)]
    public string DisplayName { get; set; } = string.Empty;

    [Required(ErrorMessage = "Vui lòng chọn quyền.")]
    public string Role { get; set; } = ShowroomRoles.Staff;

    [MinLength(8, ErrorMessage = "Mật khẩu tối thiểu 8 ký tự.")]
    [MaxLength(200)]
    [DataType(DataType.Password)]
    public string? Password { get; set; }

    [DataType(DataType.Password)]
    [Compare(nameof(Password), ErrorMessage = "Xác nhận mật khẩu không khớp.")]
    public string? ConfirmPassword { get; set; }

    public IReadOnlyList<SelectListItem> RoleOptions { get; set; } =
        new[]
        {
            new SelectListItem { Value = ShowroomRoles.Staff, Text = "Nhân viên" },
            new SelectListItem { Value = ShowroomRoles.Administrator, Text = "Quản trị viên" }
        };

    public bool RequiresPassword { get; set; }
}

