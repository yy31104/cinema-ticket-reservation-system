using Microsoft.AspNetCore.Identity;
using System.ComponentModel.DataAnnotations;

namespace MVC.Models;

public class ApplicationUser : IdentityUser
{
    [StringLength(100)]
    public string? Name { get; set; }

    [StringLength(100)]
    public string? Surname { get; set; }

    public byte[] RowVersion { get; set; } = Guid.NewGuid().ToByteArray();

    public ICollection<Reservation> Reservations { get; set; } = new List<Reservation>();
}
