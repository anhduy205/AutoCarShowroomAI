using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.Rendering;
using Showroom.Web.Security;

namespace Showroom.Web.Models;

public sealed class StaffUserFormViewModel
{
    public int Id { get; set; }

    [Required(ErrorMessage = "Ten dang nhap khong duoc de trong.")]
    [MaxLength(100)]
    public string Username { get; set; } = string.Empty;

    [Required(ErrorMessage = "Ten hien thi khong duoc de trong.")]
    [MaxLength(150)]
    public string DisplayName { get; set; } = string.Empty;

    [Required(ErrorMessage = "Vui long chon quyen.")]
    public string Role { get; set; } = ShowroomRoles.Staff;

    [MinLength(8, ErrorMessage = "Mat khau toi thieu 8 ky tu.")]
    [MaxLength(200)]
    [DataType(DataType.Password)]
    public string? Password { get; set; }

    [DataType(DataType.Password)]
    [Compare(nameof(Password), ErrorMessage = "Xac nhan mat khau khong khop.")]
    public string? ConfirmPassword { get; set; }

    public IReadOnlyList<SelectListItem> RoleOptions { get; set; } =
        new[]
        {
            new SelectListItem { Value = ShowroomRoles.Staff, Text = "Nhan vien" },
            new SelectListItem { Value = ShowroomRoles.Administrator, Text = "Quan tri vien" }
        };

    public bool RequiresPassword { get; set; }
}

