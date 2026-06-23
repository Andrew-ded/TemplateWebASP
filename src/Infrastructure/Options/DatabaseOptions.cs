using System.ComponentModel.DataAnnotations;

namespace Infrastructure.Options;

public class DatabaseOptions
{
    public const string SectionName = "Database";

    [Required]
    public string ConnectionString { get; set; } = string.Empty;

    public bool UseInMemory { get; set; }
}
