using System;
using System.Collections.Generic;

namespace Showroom.Web.Models;

public partial class OrderItem
{
    public int Id { get; set; }

    public int OrderId { get; set; }

    public int CarId { get; set; }

    public int Quantity { get; set; }

    public decimal UnitPrice { get; set; }

    public virtual Car Car { get; set; } = null!;

    public virtual Order Order { get; set; } = null!;
}
