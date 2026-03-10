using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TerminalDirectory.Entities;

namespace TerminalDirectory.Configurations;

public class OfficeConfiguration : IEntityTypeConfiguration<Office>
{
    public void Configure(EntityTypeBuilder<Office> builder)
    {
        builder.HasKey(x => x.Id);

        builder.HasIndex(x => x.Code);
        builder.HasIndex(x => x.CityCode);

        builder.Property(x => x.CountryCode).IsRequired();
        builder.Property(x => x.WorkTime).HasMaxLength(100);

        // Вложенные объекты
        builder.OwnsOne(x => x.Address, a =>
        {
            a.Property(p => p.Street).HasMaxLength(200);
            a.Property(p => p.City).HasMaxLength(100);
            a.Property(p => p.Region).HasMaxLength(100);
            a.Property(p => p.HouseNumber);
            a.Property(p => p.Apartment);
        });

        builder.OwnsOne(x => x.Coordinates, c =>
        {
            c.Property(p => p.Latitude);
            c.Property(p => p.Longitude);
        });

        // Один-ко-многим с телефоном
        builder.HasMany(x => x.Phones)
               .WithOne(p => p.Office)
               .HasForeignKey(p => p.OfficeId)
               .OnDelete(DeleteBehavior.Cascade);
    }
}