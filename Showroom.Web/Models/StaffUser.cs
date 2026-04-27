using System;
using System.Collections.Generic;

namespace Showroom.Web.Models;

public partial class StaffUser
{
    public int Id { get; set; }

    public string Username { get; set; } = null!;

    public string PasswordHash { get; set; } = null!;

    public string DisplayName { get; set; } = null!;

    public string Role { get; set; } = null!;

    public DateTime CreatedAt { get; set; }
}
