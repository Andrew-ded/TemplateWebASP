using Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.DB;

// dotnet ef migrations add Init --project src/Infrastructure --startup-project src/Web
// dotnet ef database update --project src/Infrastructure --startup-project src/Web
public class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    public DbSet<SampleEntity> Samples => Set<SampleEntity>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<SampleEntity>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Name).IsRequired().HasMaxLength(200);
            entity.Property(e => e.Description).HasMaxLength(1000);
        });
    }
}
