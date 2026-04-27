using System;
using System.Collections.Generic;

namespace Showroom.Web.Models;

public partial class Car
{
    public int Id { get; set; }

    public int BrandId { get; set; }

    public string Name { get; set; } = null!;

    public decimal Price { get; set; }

    public int StockQuantity { get; set; }

    public DateTime CreatedAt { get; set; }

    public virtual Brand Brand { get; set; } = null!;

    public virtual ICollection<OrderItem> OrderItems { get; set; } = new List<OrderItem>();
}
