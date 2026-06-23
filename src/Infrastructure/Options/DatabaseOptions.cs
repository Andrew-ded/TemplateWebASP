using System.ComponentModel.DataAnnotations;

namespace Infrastructure.Options;

public class DatabaseOptions
{
    public const string SectionName = "Database";

    [Required]
    public string Provider { get; set; } = "sqlserver";

    public string ConnectionString { get; set; } = string.Empty;

    [Required]
    public string TestProvider { get; set; } = "inmemory";

    public string TestConnectionString { get; set; } = string.Empty;
}
