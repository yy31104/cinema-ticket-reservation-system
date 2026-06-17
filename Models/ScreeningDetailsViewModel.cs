namespace MVC.Models;

public class ScreeningDetailsViewModel
{
    public int ScreeningId { get; set; }
    public string FilmTitle { get; set; } = string.Empty;
    public DateTime StartTime { get; set; }
    public string CinemaName { get; set; } = string.Empty;
    public int Rows { get; set; }
    public int SeatsPerRow { get; set; }
    public IReadOnlyList<ScreeningSeatRowViewModel> SeatRows { get; set; } = new List<ScreeningSeatRowViewModel>();
}

public class ScreeningSeatRowViewModel
{
    public int RowNumber { get; set; }
    public IReadOnlyList<ScreeningSeatViewModel> Seats { get; set; } = new List<ScreeningSeatViewModel>();
}

public class ScreeningSeatViewModel
{
    public int RowNumber { get; set; }
    public int SeatNumber { get; set; }
    public SeatOccupancyStatus Status { get; set; }
}

public enum SeatOccupancyStatus
{
    Free = 0,
    ReservedByCurrentUser = 1,
    ReservedByOtherUser = 2
}
