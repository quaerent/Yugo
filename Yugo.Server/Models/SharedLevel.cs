using System.ComponentModel.DataAnnotations;

namespace Yugo.Server.Models;

public class SharedLevel
{
    public int Id { get; set; }

    [Required]
    public string Title { get; set; } = string.Empty;

    [Required]
    public string XmlData { get; set; } = string.Empty;
    public int AuthorId { get; set; }
    public User? Author { get; set; }
    public DateTime SharedAt { get; set; } = DateTime.UtcNow;
}
