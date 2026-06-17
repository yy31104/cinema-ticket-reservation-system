using System.ComponentModel.DataAnnotations;

namespace MVC.Models;

public class ProfileEditViewModel
{
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

    public string? RowVersion { get; set; }
}
