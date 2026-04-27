using System;
using System.Collections.Generic;

namespace Showroom.Web.Models;

public partial class Order
{
    public int Id { get; set; }

    public string CustomerName { get; set; } = null!;

    public string Status { get; set; } = null!;

    public DateTime CreatedAt { get; set; }

    public virtual ICollection<OrderItem> OrderItems { get; set; } = new List<OrderItem>();
}
