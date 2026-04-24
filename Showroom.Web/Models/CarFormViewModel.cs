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

    [Range(typeof(decimal), "0", "79228162514264337593543950335", ErrorMessage = "Gia ban phai lon hon hoac bang 0.")]
    [Display(Name = "Gia ban")]
    public decimal Price { get; set; }

    [Range(0, int.MaxValue, ErrorMessage = "So luong ton phai lon hon hoac bang 0.")]
    [Display(Name = "So luong ton")]
    public int StockQuantity { get; set; }

    public IReadOnlyList<SelectListItem> BrandOptions { get; set; } = Array.Empty<SelectListItem>();
}
