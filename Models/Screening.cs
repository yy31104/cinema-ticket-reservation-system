using System.ComponentModel.DataAnnotations;

namespace MVC.Models;

public class Screening
{
    public int Id { get; set; }

    [Required]
    [StringLength(200)]
    public string FilmTitle { get; set; } = string.Empty;

    /// <summary>
    /// Local wall-clock time of the screening at its cinema; stored unconverted. See ADR-0006.
    /// </summary>
    [DataType(DataType.DateTime)]
    public DateTime StartTime { get; set; }

    [StringLength(2048)]
    public string? PosterUrl { get; set; }

    [StringLength(2000)]
    public string? Synopsis { get; set; }

    [Range(1, 600)]
    public int? DurationMinutes { get; set; }

    [StringLength(60)]
    public string? Genre { get; set; }

    [StringLength(16)]
    public string? AgeRating { get; set; }

    [Required]
    public int CinemaId { get; set; }

    public Cinema Cinema { get; set; } = null!;

    public ICollection<Reservation> Reservations { get; set; } = new List<Reservation>();
}
