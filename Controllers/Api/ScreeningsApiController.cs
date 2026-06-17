using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using MVC.Data;
using MVC.Models;
using MVC.Models.Api;

namespace MVC.Controllers.Api;

[ApiController]
[Route("api/screenings")]
public class ScreeningsApiController : ControllerBase
{
    private const string FreeSeatStatus = "free";
    private const string ReservedByCurrentUserStatus = "reservedByCurrentUser";
    private const string ReservedByOtherUserStatus = "reservedByOtherUser";

    private readonly ApplicationDbContext _context;

    public ScreeningsApiController(ApplicationDbContext context)
    {
        _context = context;
    }

    [HttpGet]
    [AllowAnonymous]
    public async Task<IActionResult> GetScreenings()
    {
        var screenings = await _context.Screenings
            .AsNoTracking()
            .Include(s => s.Cinema)
            .Include(s => s.Reservations)
            .OrderBy(s => s.StartTime)
            .Select(s => new ScreeningListItemDto
            {
                Id = s.Id,
                FilmTitle = s.FilmTitle,
                StartTime = s.StartTime,
                Cinema = new CinemaDto
                {
                    Id = s.Cinema.Id,
                    Name = s.Cinema.Name,
                    Rows = s.Cinema.Rows,
                    SeatsPerRow = s.Cinema.SeatsPerRow
                },
                ReservationCount = s.Reservations.Count
            })
            .ToListAsync();

        return Ok(screenings);
    }

    [HttpGet("cinemas")]
    [AllowAnonymous]
    public async Task<IActionResult> GetCinemas()
    {
        var cinemas = await _context.Cinemas
            .AsNoTracking()
            .OrderBy(c => c.Name)
            .Select(c => new CinemaDto
            {
                Id = c.Id,
                Name = c.Name,
                Rows = c.Rows,
                SeatsPerRow = c.SeatsPerRow
            })
            .ToListAsync();

        return Ok(cinemas);
    }

    [HttpGet("{id:int}")]
    [AllowAnonymous]
    public async Task<IActionResult> GetScreening(int id)
    {
        var screening = await _context.Screenings
            .AsNoTracking()
            .Include(s => s.Cinema)
            .Include(s => s.Reservations)
            .SingleOrDefaultAsync(s => s.Id == id);

        if (screening is null)
        {
            return NotFound(new ApiErrorDto { Message = "Screening not found." });
        }

        return Ok(ToScreeningDetailsDto(screening));
    }

    [HttpGet("{id:int}/seats")]
    [Authorize]
    public async Task<IActionResult> GetSeats(int id)
    {
        var screening = await _context.Screenings
            .AsNoTracking()
            .Include(s => s.Cinema)
            .Include(s => s.Reservations)
            .SingleOrDefaultAsync(s => s.Id == id);

        if (screening is null)
        {
            return NotFound(new ApiErrorDto { Message = "Screening not found." });
        }

        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrWhiteSpace(userId))
        {
            return Unauthorized(new ApiErrorDto { Message = "Authentication is required." });
        }

        return Ok(BuildSeatMapDto(screening, userId, User.IsInRole("Admin")));
    }

    [HttpPost]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> CreateScreening(CreateScreeningRequestDto request)
    {
        if (string.IsNullOrWhiteSpace(request.FilmTitle))
        {
            return BadRequest(new ApiErrorDto { Message = "Film title is required." });
        }

        if (request.StartTime == default)
        {
            return BadRequest(new ApiErrorDto { Message = "Start time is required." });
        }

        var cinema = await _context.Cinemas.SingleOrDefaultAsync(c => c.Id == request.CinemaId);
        if (cinema is null)
        {
            return BadRequest(new ApiErrorDto { Message = "Selected cinema does not exist." });
        }

        var screening = new Screening
        {
            FilmTitle = request.FilmTitle.Trim(),
            StartTime = request.StartTime,
            CinemaId = request.CinemaId,
            Cinema = cinema
        };

        _context.Screenings.Add(screening);
        await _context.SaveChangesAsync();

        return CreatedAtAction(
            nameof(GetScreening),
            new { id = screening.Id },
            ToScreeningDetailsDto(screening));
    }

    [HttpDelete("{id:int}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> DeleteScreening(int id)
    {
        var screening = await _context.Screenings
            .Include(s => s.Reservations)
            .SingleOrDefaultAsync(s => s.Id == id);

        if (screening is null)
        {
            return NotFound(new ApiErrorDto { Message = "Screening not found." });
        }

        _context.Screenings.Remove(screening);
        await _context.SaveChangesAsync();

        return Ok(new { message = "Screening and its reservations were deleted successfully." });
    }

    [HttpPost("{id:int}/reservations")]
    [Authorize]
    public async Task<IActionResult> ReserveSeat(int id, ReserveSeatRequestDto request)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrWhiteSpace(userId))
        {
            return Unauthorized(new ApiErrorDto { Message = "Authentication is required." });
        }

        var screening = await _context.Screenings
            .AsNoTracking()
            .Include(s => s.Cinema)
            .SingleOrDefaultAsync(s => s.Id == id);

        if (screening is null)
        {
            return NotFound(new ApiErrorDto { Message = "Screening not found." });
        }

        if (!IsSeatInsideCinema(screening.Cinema, request.RowNumber, request.SeatNumber))
        {
            return BadRequest(new ApiErrorDto { Message = "Selected seat is outside auditorium bounds." });
        }

        var reservation = new Reservation
        {
            ScreeningId = id,
            UserId = userId,
            RowNumber = request.RowNumber,
            SeatNumber = request.SeatNumber,
            CreatedAt = DateTime.UtcNow
        };

        _context.Reservations.Add(reservation);

        try
        {
            await _context.SaveChangesAsync();
            return StatusCode(StatusCodes.Status201Created, ToReservationDto(reservation, userId));
        }
        catch (DbUpdateException ex) when (IsSeatUniqueConstraintViolation(ex))
        {
            return Conflict(new ApiErrorDto { Message = "This seat was already reserved by another user." });
        }
        catch (DbUpdateException)
        {
            return Conflict(new ApiErrorDto { Message = "Unable to reserve this seat right now." });
        }
    }

    [HttpDelete("{id:int}/reservations/{reservationId:int}")]
    [Authorize]
    public async Task<IActionResult> CancelReservation(int id, int reservationId)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrWhiteSpace(userId))
        {
            return Unauthorized(new ApiErrorDto { Message = "Authentication is required." });
        }

        var reservation = await _context.Reservations
            .SingleOrDefaultAsync(r => r.Id == reservationId && r.ScreeningId == id);

        if (reservation is null)
        {
            return NotFound(new ApiErrorDto { Message = "Reservation not found." });
        }

        var isAdmin = User.IsInRole("Admin");
        if (reservation.UserId != userId && !isAdmin)
        {
            return StatusCode(StatusCodes.Status403Forbidden, new ApiErrorDto { Message = "You can cancel only your own reservation." });
        }

        _context.Reservations.Remove(reservation);
        await _context.SaveChangesAsync();

        return Ok(new { message = "Reservation cancelled successfully." });
    }

    private static ScreeningDetailsDto ToScreeningDetailsDto(Screening screening)
    {
        return new ScreeningDetailsDto
        {
            Id = screening.Id,
            FilmTitle = screening.FilmTitle,
            StartTime = screening.StartTime,
            Cinema = ToCinemaDto(screening.Cinema),
            ReservationCount = screening.Reservations.Count
        };
    }

    private static CinemaDto ToCinemaDto(Cinema cinema)
    {
        return new CinemaDto
        {
            Id = cinema.Id,
            Name = cinema.Name,
            Rows = cinema.Rows,
            SeatsPerRow = cinema.SeatsPerRow
        };
    }

    private static SeatMapDto BuildSeatMapDto(Screening screening, string currentUserId, bool isAdmin)
    {
        var reservationBySeat = screening.Reservations
            .ToDictionary(r => (r.RowNumber, r.SeatNumber));

        var seatRows = new List<SeatRowDto>();
        for (var rowNumber = 1; rowNumber <= screening.Cinema.Rows; rowNumber++)
        {
            var seats = new List<SeatDto>();
            for (var seatNumber = 1; seatNumber <= screening.Cinema.SeatsPerRow; seatNumber++)
            {
                reservationBySeat.TryGetValue((rowNumber, seatNumber), out var reservation);
                var reservedByCurrentUser = reservation?.UserId == currentUserId;
                var status = reservation is null
                    ? FreeSeatStatus
                    : reservedByCurrentUser ? ReservedByCurrentUserStatus : ReservedByOtherUserStatus;

                seats.Add(new SeatDto
                {
                    RowNumber = rowNumber,
                    SeatNumber = seatNumber,
                    Status = status,
                    ReservationId = reservation?.Id,
                    ReservedByCurrentUser = reservedByCurrentUser,
                    CanCancel = reservation is not null && (reservedByCurrentUser || isAdmin)
                });
            }

            seatRows.Add(new SeatRowDto
            {
                RowNumber = rowNumber,
                Seats = seats
            });
        }

        return new SeatMapDto
        {
            ScreeningId = screening.Id,
            FilmTitle = screening.FilmTitle,
            StartTime = screening.StartTime,
            Cinema = ToCinemaDto(screening.Cinema),
            SeatRows = seatRows
        };
    }

    private static ReservationDto ToReservationDto(Reservation reservation, string currentUserId)
    {
        return new ReservationDto
        {
            Id = reservation.Id,
            ScreeningId = reservation.ScreeningId,
            RowNumber = reservation.RowNumber,
            SeatNumber = reservation.SeatNumber,
            CreatedAt = reservation.CreatedAt,
            ReservedByCurrentUser = reservation.UserId == currentUserId
        };
    }

    private static bool IsSeatInsideCinema(Cinema cinema, int rowNumber, int seatNumber)
    {
        return rowNumber >= 1 &&
            rowNumber <= cinema.Rows &&
            seatNumber >= 1 &&
            seatNumber <= cinema.SeatsPerRow;
    }

    private static bool IsSeatUniqueConstraintViolation(DbUpdateException ex)
    {
        if (ex.InnerException is not SqliteException sqliteEx)
        {
            return false;
        }

        const int sqliteConstraintErrorCode = 19;
        if (sqliteEx.SqliteErrorCode != sqliteConstraintErrorCode)
        {
            return false;
        }

        return sqliteEx.Message.Contains("IX_Reservations_ScreeningId_RowNumber_SeatNumber", StringComparison.OrdinalIgnoreCase)
            || sqliteEx.Message.Contains("UNIQUE constraint failed: Reservations.ScreeningId, Reservations.RowNumber, Reservations.SeatNumber", StringComparison.OrdinalIgnoreCase);
    }
}
