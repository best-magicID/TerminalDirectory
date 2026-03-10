using System.Reflection;
using Microsoft.EntityFrameworkCore;
using TerminalDirectory.Entities;

namespace TerminalDirectory.Infrastructure;

public class DellinDictionaryDbContext : DbContext
{
    public DellinDictionaryDbContext(DbContextOptions<DellinDictionaryDbContext> options)
        : base(options)
    {
    }

    public DbSet<Office> Offices => Set<Office>();
    public DbSet<Phone> Phones => Set<Phone>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);
        builder.ApplyConfigurationsFromAssembly(Assembly.GetExecutingAssembly());
    }
}
