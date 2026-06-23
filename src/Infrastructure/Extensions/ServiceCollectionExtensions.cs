using Domain.Interfaces;
using Infrastructure.DB;
using Infrastructure.Options;
using Infrastructure.Repository;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Infrastructure.Extensions;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration,
        bool useTestDb = false)
    {
        services.AddOptions<DatabaseOptions>()
            .Bind(configuration.GetSection(DatabaseOptions.SectionName))
            .ValidateOnStart();

        var dbOptions = configuration
            .GetSection(DatabaseOptions.SectionName)
            .Get<DatabaseOptions>() ?? new DatabaseOptions();

        var provider = useTestDb ? dbOptions.TestProvider : dbOptions.Provider;
        var connectionString = useTestDb ? dbOptions.TestConnectionString : dbOptions.ConnectionString;

        services.AddDbContext<AppDbContext>(options =>
            ConfigureProvider(options, provider, connectionString));

        services.AddScoped(typeof(IRepository<>), typeof(GenericRepository<>));

        return services;
    }

    private static void ConfigureProvider(
        DbContextOptionsBuilder options,
        string provider,
        string connectionString)
    {
        switch (provider.ToLowerInvariant())
        {
            case "sqlserver":
                options.UseSqlServer(connectionString);
                break;

            case "mysql":
                options.UseMySql(connectionString, ServerVersion.AutoDetect(connectionString));
                break;

            case "sqlite":
                options.UseSqlite(
                    string.IsNullOrEmpty(connectionString)
                        ? "Data Source=:memory:"
                        : connectionString);
                break;

            case "inmemory":
                options.UseInMemoryDatabase("TemplateDb");
                break;

            default:
                throw new InvalidOperationException(
                    $"Unsupported database provider: '{provider}'. " +
                    "Supported: sqlserver, mysql, sqlite, inmemory");
        }
    }
}
