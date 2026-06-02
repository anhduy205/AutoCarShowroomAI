using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.Rendering;
using Showroom.Web.Validation;

namespace Showroom.Web.Models;

public sealed class BranchFormViewModel
{
    public int Id { get; set; }

    [Required(ErrorMessage = "Vui lòng nhập mã chi nhánh.")]
    [NotWhiteSpace(ErrorMessage = "Mã chi nhánh không được chỉ gồm khoảng trắng.")]
    [StringLength(30, ErrorMessage = "Mã chi nhánh tối đa 30 ký tự.")]
    [Display(Name = "Mã chi nhánh")]
    public string BranchCode { get; set; } = string.Empty;

    [Required(ErrorMessage = "Vui lòng nhập tên chi nhánh.")]
    [NotWhiteSpace(ErrorMessage = "Tên chi nhánh không được chỉ gồm khoảng trắng.")]
    [StringLength(150, ErrorMessage = "Tên chi nhánh tối đa 150 ký tự.")]
    [Display(Name = "Tên chi nhánh")]
    public string Name { get; set; } = string.Empty;

    [Required(ErrorMessage = "Vui lòng chọn trạng thái.")]
    [Display(Name = "Trạng thái")]
    public string Status { get; set; } = BranchStatusCatalog.Active;

    public IReadOnlyList<SelectListItem> StatusOptions { get; set; } =
        BranchStatusCatalog.GetSelectList(BranchStatusCatalog.Active);
}
