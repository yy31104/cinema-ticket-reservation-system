using System.ComponentModel.DataAnnotations;

namespace MVC.Models;

public class Cinema
{
    public int Id { get; set; }

    [Required]
    [StringLength(100)]
    public string Name { get; set; } = string.Empty;

    [Range(1, 100)]
    public int Rows { get; set; }

    [Range(1, 100)]
    public int SeatsPerRow { get; set; }

    public ICollection<Screening> Screenings { get; set; } = new List<Screening>();
}
