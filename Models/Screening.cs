using System.ComponentModel.DataAnnotations;

namespace MVC.Models;

public class Screening
{
    public int Id { get; set; }

    [Required]
    [StringLength(200)]
    public string FilmTitle { get; set; } = string.Empty;

    [DataType(DataType.DateTime)]
    public DateTime StartTime { get; set; }

    [Required]
    public int CinemaId { get; set; }

    public Cinema Cinema { get; set; } = null!;

    public ICollection<Reservation> Reservations { get; set; } = new List<Reservation>();
}
