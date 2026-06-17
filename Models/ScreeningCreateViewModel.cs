using System.ComponentModel.DataAnnotations;

namespace MVC.Models;

public class ScreeningCreateViewModel
{
    [Required]
    [StringLength(200)]
    [Display(Name = "Film title")]
    public string FilmTitle { get; set; } = string.Empty;

    [Required]
    [DataType(DataType.DateTime)]
    [Display(Name = "Start time")]
    public DateTime StartTime { get; set; } = DateTime.Now.AddHours(1);

    [Required]
    [Display(Name = "Cinema")]
    public int CinemaId { get; set; }
}
