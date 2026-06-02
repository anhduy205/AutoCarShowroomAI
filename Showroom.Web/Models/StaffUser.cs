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

    public int? BranchId { get; set; }

    public string? BranchCode { get; set; }

    public string? BranchName { get; set; }

    public string? StaffCode { get; set; }

    public string? Email { get; set; }

    public string? Phone { get; set; }

    public bool IsActive { get; set; } = true;

    public DateTime CreatedAt { get; set; }
}
