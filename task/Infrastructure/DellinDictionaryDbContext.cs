using Microsoft.EntityFrameworkCore;
using System.Reflection;
using TerminalDirectory.Entities;

namespace TerminalDirectory.Infrastructure;

public class DellinDictionaryDbContext : DbContext
{
    public DellinDictionaryDbContext(DbContextOptions<DellinDictionaryDbContext> options) 
        : base(options) 
    { 

    }

    public DbSet<Office> Offices { get; set; }
    public DbSet<Phone> Phones { get; set; }


    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        builder.Entity<Office>(entity =>
        {
            entity.HasIndex(o => o.CityCode);
            entity.HasIndex(o => o.Code);

            entity.OwnsOne(o => o.Coordinates);
            entity.OwnsOne(o => o.Phones);
        });

        builder.Entity<Phone>(entity =>
        {
            entity.HasIndex(p => p.OfficeId);
        });

        builder.ApplyConfigurationsFromAssembly(Assembly.GetExecutingAssembly());
    }
}
