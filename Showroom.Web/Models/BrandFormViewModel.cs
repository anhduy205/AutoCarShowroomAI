using System.ComponentModel.DataAnnotations;
using Showroom.Web.Validation;

namespace Showroom.Web.Models;

public class BrandFormViewModel
{
    public int Id { get; set; }

    [Required(ErrorMessage = "Vui long nhap ten hang xe.")]
    [NotWhiteSpace(ErrorMessage = "Ten hang xe khong duoc chi gom khoang trang.")]
    [StringLength(100, ErrorMessage = "Ten hang xe toi da 100 ky tu.")]
    [Display(Name = "Ten hang xe")]
    public string Name { get; set; } = string.Empty;
}
