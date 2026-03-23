using System.ComponentModel.DataAnnotations;

namespace Showroom.Web.Models;

public class BrandFormViewModel
{
    public int Id { get; set; }

    [Required(ErrorMessage = "Vui lòng nhập tên hãng xe.")]
    [StringLength(100, ErrorMessage = "Tên hãng xe tối đa 100 ký tự.")]
    [Display(Name = "Tên hãng xe")]
    public string Name { get; set; } = string.Empty;
}
