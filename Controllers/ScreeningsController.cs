using System.Security.Claims;
using Microsoft.Data.Sqlite;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using MVC.Data;
using MVC.Models;

namespace MVC.Controllers;

[Authorize]
public class ScreeningsController : Controller
{
    private readonly ApplicationDbContext _context;

    public ScreeningsController(ApplicationDbContext context)
    {
        _context = context;
    }

    [AllowAnonymous]
    public async Task<IActionResult> Index()
    {
        var screenings = await _context.Screenings
            .Include(s => s.Cinema)
            .OrderBy(s => s.StartTime)
            .ToListAsync();

        return View(screenings);
    }

    [HttpGet]
    public async Task<IActionResult> Details(int? id)
    {
        if (id is null)
        {
            return NotFound();
        }

        var screening = await _context.Screenings
            .Include(s => s.Cinema)
            .Include(s => s.Reservations)
            .FirstOrDefaultAsync(s => s.Id == id.Value);

        if (screening is null)
        {
            return NotFound();
        }

        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(userId))
        {
            return Challenge();
        }

        var model = BuildDetailsViewModel(screening, userId);
        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Reserve(int id, int rowNumber, int seatNumber)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(userId))
        {
            return Challenge();
        }

        var screening = await _context.Screenings
            .Include(s => s.Cinema)
            .FirstOrDefaultAsync(s => s.Id == id);

        if (screening is null)
        {
            return NotFound();
        }

        if (rowNumber < 1 || rowNumber > screening.Cinema.Rows || seatNumber < 1 || seatNumber > screening.Cinema.SeatsPerRow)
        {
            TempData["ErrorMessage"] = "Selected seat is outside auditorium bounds.";
            return RedirectToAction(nameof(Details), new { id });
        }

        var reservation = new Reservation
        {
            ScreeningId = id,
            UserId = userId,
            RowNumber = rowNumber,
            SeatNumber = seatNumber,
            CreatedAt = DateTime.UtcNow
        };

        try
        {
            _context.Reservations.Add(reservation);
            await _context.SaveChangesAsync();
            TempData["StatusMessage"] = $"Seat R{rowNumber} S{seatNumber} reserved.";
        }
        catch (DbUpdateException ex) when (IsSeatUniqueConstraintViolation(ex))
        {
            TempData["ErrorMessage"] = "This seat was just taken by another user. Please choose another seat.";
        }
        catch (DbUpdateException)
        {
            TempData["ErrorMessage"] = "Unable to reserve this seat right now. Please try again.";
        }

        return RedirectToAction(nameof(Details), new { id });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Release(int id, int rowNumber, int seatNumber)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(userId))
        {
            return Challenge();
        }

        var reservation = await _context.Reservations.FirstOrDefaultAsync(r =>
            r.ScreeningId == id &&
            r.RowNumber == rowNumber &&
            r.SeatNumber == seatNumber &&
            r.UserId == userId);

        if (reservation is null)
        {
            TempData["ErrorMessage"] = "You can only release your own reservation.";
            return RedirectToAction(nameof(Details), new { id });
        }

        _context.Reservations.Remove(reservation);
        await _context.SaveChangesAsync();
        TempData["StatusMessage"] = $"Seat R{rowNumber} S{seatNumber} released.";
        return RedirectToAction(nameof(Details), new { id });
    }

    [Authorize(Roles = "Admin")]
    [HttpGet]
    public async Task<IActionResult> Create()
    {
        await PopulateCinemasAsync();
        return View(new ScreeningCreateViewModel());
    }

    [Authorize(Roles = "Admin")]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(ScreeningCreateViewModel model)
    {
        if (!ModelState.IsValid)
        {
            await PopulateCinemasAsync(model.CinemaId);
            return View(model);
        }

        var screening = new Screening
        {
            FilmTitle = model.FilmTitle.Trim(),
            StartTime = model.StartTime,
            CinemaId = model.CinemaId
        };

        _context.Screenings.Add(screening);
        await _context.SaveChangesAsync();
        return RedirectToAction(nameof(Index));
    }

    [Authorize(Roles = "Admin")]
    [HttpGet]
    public async Task<IActionResult> Delete(int? id)
    {
        if (id is null)
        {
            return NotFound();
        }

        var screening = await _context.Screenings
            .Include(s => s.Cinema)
            .FirstOrDefaultAsync(s => s.Id == id.Value);

        if (screening is null)
        {
            return NotFound();
        }

        return View(screening);
    }

    [Authorize(Roles = "Admin")]
    [HttpPost, ActionName("Delete")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteConfirmed(int id)
    {
        var screening = await _context.Screenings.FindAsync(id);
        if (screening is not null)
        {
            _context.Screenings.Remove(screening);
            await _context.SaveChangesAsync();
        }

        return RedirectToAction(nameof(Index));
    }

    private async Task PopulateCinemasAsync(int? selectedCinemaId = null)
    {
        var cinemas = await _context.Cinemas
            .OrderBy(c => c.Name)
            .ToListAsync();

        ViewBag.CinemaId = new SelectList(cinemas, nameof(Cinema.Id), nameof(Cinema.Name), selectedCinemaId);
    }

    private static ScreeningDetailsViewModel BuildDetailsViewModel(Screening screening, string currentUserId)
    {
        var reservationBySeat = screening.Reservations
            .ToDictionary(r => (r.RowNumber, r.SeatNumber), r => r.UserId);

        var seatRows = new List<ScreeningSeatRowViewModel>();
        for (var rowNumber = 1; rowNumber <= screening.Cinema.Rows; rowNumber++)
        {
            var seats = new List<ScreeningSeatViewModel>();
            for (var seatNumber = 1; seatNumber <= screening.Cinema.SeatsPerRow; seatNumber++)
            {
                var status = SeatOccupancyStatus.Free;

                if (reservationBySeat.TryGetValue((rowNumber, seatNumber), out var reservedByUserId))
                {
                    status = reservedByUserId == currentUserId
                        ? SeatOccupancyStatus.ReservedByCurrentUser
                        : SeatOccupancyStatus.ReservedByOtherUser;
                }

                seats.Add(new ScreeningSeatViewModel
                {
                    RowNumber = rowNumber,
                    SeatNumber = seatNumber,
                    Status = status
                });
            }

            seatRows.Add(new ScreeningSeatRowViewModel
            {
                RowNumber = rowNumber,
                Seats = seats
            });
        }

        return new ScreeningDetailsViewModel
        {
            ScreeningId = screening.Id,
            FilmTitle = screening.FilmTitle,
            StartTime = screening.StartTime,
            CinemaName = screening.Cinema.Name,
            Rows = screening.Cinema.Rows,
            SeatsPerRow = screening.Cinema.SeatsPerRow,
            SeatRows = seatRows
        };
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
