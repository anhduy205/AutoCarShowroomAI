using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.Rendering;
using Showroom.Web.Validation;

namespace Showroom.Web.Models;

public class CarFormViewModel
{
    public int Id { get; set; }

    [Range(1, int.MaxValue, ErrorMessage = "Vui long chon hang xe.")]
    [Display(Name = "Hang xe")]
    public int BrandId { get; set; }

    [Required(ErrorMessage = "Vui long nhap ten xe.")]
    [NotWhiteSpace(ErrorMessage = "Ten xe khong duoc chi gom khoang trang.")]
    [StringLength(150, ErrorMessage = "Ten xe toi da 150 ky tu.")]
    [Display(Name = "Ten xe")]
    public string Name { get; set; } = string.Empty;

    [Range(1900, 2100, ErrorMessage = "Nam san xuat phai nam trong khoang 1900-2100.")]
    [Display(Name = "Nam san xuat")]
    public int? Year { get; set; }

    [StringLength(50, ErrorMessage = "Loai xe toi da 50 ky tu.")]
    [Display(Name = "Loai xe")]
    public string? Type { get; set; }

    [StringLength(50, ErrorMessage = "Mau sac toi da 50 ky tu.")]
    [Display(Name = "Mau sac")]
    public string? Color { get; set; }

    [StringLength(1000, ErrorMessage = "Mo ta toi da 1000 ky tu.")]
    [Display(Name = "Mo ta")]
    public string? Description { get; set; }

    [StringLength(8000, ErrorMessage = "Thong so ky thuat toi da 8000 ky tu.")]
    [Display(Name = "Thong so ky thuat")]
    public string? Specifications { get; set; }

    [StringLength(4000, ErrorMessage = "Danh sach anh toi da 4000 ky tu.")]
    [Display(Name = "Anh (moi dong 1 URL)")]
    public string? ImageUrls { get; set; }

    [Required(ErrorMessage = "Vui long chon trang thai xe.")]
    [Display(Name = "Trang thai")]
    public string Status { get; set; } = CarStatusCatalog.InStock;

    [Range(typeof(decimal), "0", "79228162514264337593543950335", ErrorMessage = "Gia ban phai lon hon hoac bang 0.")]
    [Display(Name = "Gia ban")]
    public decimal Price { get; set; }

    [Range(0, int.MaxValue, ErrorMessage = "So luong ton phai lon hon hoac bang 0.")]
    [Display(Name = "So luong ton")]
    public int StockQuantity { get; set; }

    public IReadOnlyList<SelectListItem> BrandOptions { get; set; } = Array.Empty<SelectListItem>();

    public IReadOnlyList<SelectListItem> StatusOptions { get; set; } = CarStatusCatalog.GetSelectList();
}
