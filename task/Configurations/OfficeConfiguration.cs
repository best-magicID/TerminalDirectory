using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TerminalDirectory.Entities;

namespace TerminalDirectory.Configurations;

public class OfficeConfiguration : IEntityTypeConfiguration<Office>
{
    public void Configure(EntityTypeBuilder<Office> builder)
    {
        // Первичный ключ
        builder.HasKey(x => x.Id);

        // Индексы
        builder.HasIndex(x => x.Code);
        builder.HasIndex(x => x.CityCode);

        builder.Property(x => x.CountryCode)
               .IsRequired();

        builder.Property(x => x.WorkTime)
               .HasMaxLength(100);

        builder.Property(x => x.AddressStreet)
               .HasMaxLength(200);

        builder.Property(x => x.AddressCity)
               .HasMaxLength(100);

        builder.Property(x => x.AddressRegion)
               .HasMaxLength(100);

        // Координаты
        builder.OwnsOne(x => x.Coordinates, c =>
        {
            c.Property(p => p.Latitude);
            c.Property(p => p.Longitude);
        });

        // Связь
        builder.HasMany(x => x.Phones)
               .WithOne(p => p.Office)
               .HasForeignKey(p => p.OfficeId)
               .OnDelete(DeleteBehavior.Cascade);
    }
}