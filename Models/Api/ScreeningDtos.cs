using System.ComponentModel.DataAnnotations;

namespace MVC.Models.Api;

public class CinemaDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public int Rows { get; set; }
    public int SeatsPerRow { get; set; }
}

public class ScreeningListItemDto
{
    public int Id { get; set; }
    public string FilmTitle { get; set; } = string.Empty;
    public DateTime StartTime { get; set; }
    public string? PosterUrl { get; set; }
    public string? Synopsis { get; set; }
    public int? DurationMinutes { get; set; }
    public string? Genre { get; set; }
    public string? AgeRating { get; set; }
    public CinemaDto Cinema { get; set; } = new();
    public int ReservationCount { get; set; }
}

public class ScreeningDetailsDto
{
    public int Id { get; set; }
    public string FilmTitle { get; set; } = string.Empty;
    public DateTime StartTime { get; set; }
    public string? PosterUrl { get; set; }
    public string? Synopsis { get; set; }
    public int? DurationMinutes { get; set; }
    public string? Genre { get; set; }
    public string? AgeRating { get; set; }
    public CinemaDto Cinema { get; set; } = new();
    public int ReservationCount { get; set; }
}

public class CreateScreeningRequestDto
{
    [Required]
    [StringLength(200)]
    public string FilmTitle { get; set; } = string.Empty;

    [Required]
    public DateTime StartTime { get; set; }

    [Required]
    public int CinemaId { get; set; }

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
}

public class SeatMapDto
{
    public int ScreeningId { get; set; }
    public string FilmTitle { get; set; } = string.Empty;
    public DateTime StartTime { get; set; }
    public CinemaDto Cinema { get; set; } = new();
    public IReadOnlyList<SeatRowDto> SeatRows { get; set; } = Array.Empty<SeatRowDto>();
}

public class SeatRowDto
{
    public int RowNumber { get; set; }
    public IReadOnlyList<SeatDto> Seats { get; set; } = Array.Empty<SeatDto>();
}

public class SeatDto
{
    public int RowNumber { get; set; }
    public int SeatNumber { get; set; }
    public string Status { get; set; } = string.Empty;
    public int? ReservationId { get; set; }
    public bool ReservedByCurrentUser { get; set; }
    public bool CanCancel { get; set; }
}

public class ReserveSeatRequestDto
{
    [Range(1, 100)]
    public int RowNumber { get; set; }

    [Range(1, 100)]
    public int SeatNumber { get; set; }
}

public class ReservationDto
{
    public int Id { get; set; }
    public int ScreeningId { get; set; }
    public int RowNumber { get; set; }
    public int SeatNumber { get; set; }
    public DateTime CreatedAt { get; set; }
    public bool ReservedByCurrentUser { get; set; }
}
