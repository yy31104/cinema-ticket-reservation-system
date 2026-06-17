using System.ComponentModel.DataAnnotations;

namespace MVC.Models;

public class AdminUserListItemViewModel
{
    public string Id { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string? Name { get; set; }
    public string? Surname { get; set; }
}

public class AdminUserDeleteViewModel
{
    [Required]
    public string Id { get; set; } = string.Empty;

    public string Email { get; set; } = string.Empty;
    public string? Name { get; set; }
    public string? Surname { get; set; }
    public string? PhoneNumber { get; set; }
    public string RowVersion { get; set; } = string.Empty;
}

public class AdminUserEditViewModel
{
    [Required]
    public string Id { get; set; } = string.Empty;

    public string Email { get; set; } = string.Empty;

    [Display(Name = "Name")]
    [StringLength(100)]
    public string? Name { get; set; }

    [Display(Name = "Surname")]
    [StringLength(100)]
    public string? Surname { get; set; }

    [Display(Name = "Phone number")]
    [Phone]
    [StringLength(32)]
    public string? PhoneNumber { get; set; }

    public string RowVersion { get; set; } = string.Empty;
}
