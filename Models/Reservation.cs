using System.ComponentModel.DataAnnotations;

namespace MVC.Models;

public class Reservation
{
    public int Id { get; set; }

    [Required]
    public int ScreeningId { get; set; }

    public Screening Screening { get; set; } = null!;

    [Required]
    public string UserId { get; set; } = string.Empty;

    public ApplicationUser User { get; set; } = null!;

    [Range(1, 100)]
    public int RowNumber { get; set; }

    [Range(1, 100)]
    public int SeatNumber { get; set; }

    [DataType(DataType.DateTime)]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
