using Microsoft.EntityFrameworkCore;
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
        builder.Entity<Office>()
               .HasMany(o => o.Phones)
               .WithOne(p => p.Office)
               .HasForeignKey(p => p.OfficeId)
               .OnDelete(DeleteBehavior.Cascade);

        builder.Entity<Office>()
               .OwnsOne(o => o.Coordinates);
    }
}
