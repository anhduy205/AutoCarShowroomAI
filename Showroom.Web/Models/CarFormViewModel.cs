using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.Rendering;
using Showroom.Web.Validation;

namespace Showroom.Web.Models;

public class CarFormViewModel
{
    public int Id { get; set; }

    [Range(1, int.MaxValue, ErrorMessage = "Vui lòng chọn hãng xe.")]
    [Display(Name = "Hãng xe")]
    public int BrandId { get; set; }

    [Required(ErrorMessage = "Vui lòng nhập tên xe.")]
    [NotWhiteSpace(ErrorMessage = "Tên xe không được chỉ gồm khoảng trắng.")]
    [StringLength(150, ErrorMessage = "Tên xe tối đa 150 ký tự.")]
    [Display(Name = "Tên xe")]
    public string Name { get; set; } = string.Empty;

    [Range(1900, 2100, ErrorMessage = "Năm san xuat phai nam trong khoang 1900-2100.")]
    [Display(Name = "Năm san xuat")]
    public int? Year { get; set; }

    [StringLength(50, ErrorMessage = "Loại xe toi da 50 ky tu.")]
    [Display(Name = "Loại xe")]
    public string? Type { get; set; }

    [StringLength(50, ErrorMessage = "Màu sac toi da 50 ky tu.")]
    [Display(Name = "Màu sac")]
    public string? Color { get; set; }

    [StringLength(1000, ErrorMessage = "Mô tả toi da 1000 ky tu.")]
    [Display(Name = "Mô tả")]
    public string? Description { get; set; }

    [StringLength(8000, ErrorMessage = "Thông số kỹ thuật toi da 8000 ky tu.")]
    [Display(Name = "Thông số kỹ thuật")]
    public string? Specifications { get; set; }

    [StringLength(4000, ErrorMessage = "Danh sách ảnh tối đa 4000 ký tự.")]
    [Display(Name = "Ảnh (mỗi dòng 1 URL)")]
    public string? ImageUrls { get; set; }

    [Required(ErrorMessage = "Vui lòng chọn trạng thái xe.")]
    [Display(Name = "Trạng thái")]
    public string Status { get; set; } = CarStatusCatalog.InStock;

    [Range(typeof(decimal), "0", "79228162514264337593543950335", ErrorMessage = "Giá bán phải lớn hơn hoặc bằng 0.")]
    [Display(Name = "Giá ban")]
    public decimal Price { get; set; }

    [Range(0, int.MaxValue, ErrorMessage = "Số lượng tồn phải lớn hơn hoặc bằng 0.")]
    [Display(Name = "Số lượng tồn")]
    public int StockQuantity { get; set; }

    public IReadOnlyList<SelectListItem> BrandOptions { get; set; } = Array.Empty<SelectListItem>();

    public IReadOnlyList<SelectListItem> StatusOptions { get; set; } = CarStatusCatalog.GetSelectList();
}
