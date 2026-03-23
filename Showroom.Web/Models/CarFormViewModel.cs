using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace Showroom.Web.Models;

public class CarFormViewModel
{
    public int Id { get; set; }

    [Range(1, int.MaxValue, ErrorMessage = "Vui lòng chọn hãng xe.")]
    [Display(Name = "Hãng xe")]
    public int BrandId { get; set; }

    [Required(ErrorMessage = "Vui lòng nhập tên xe.")]
    [StringLength(150, ErrorMessage = "Tên xe tối đa 150 ký tự.")]
    [Display(Name = "Tên xe")]
    public string Name { get; set; } = string.Empty;

    [Range(typeof(decimal), "0", "79228162514264337593543950335", ErrorMessage = "Giá bán phải lớn hơn hoặc bằng 0.")]
    [Display(Name = "Giá bán")]
    public decimal Price { get; set; }

    [Range(0, int.MaxValue, ErrorMessage = "Số lượng tồn phải lớn hơn hoặc bằng 0.")]
    [Display(Name = "Số lượng tồn")]
    public int StockQuantity { get; set; }

    public IReadOnlyList<SelectListItem> BrandOptions { get; set; } = Array.Empty<SelectListItem>();
}
