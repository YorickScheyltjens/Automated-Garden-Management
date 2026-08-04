using GardenSystem.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GardenSystem.Infrastructure.Persistence.Configurations;

public sealed class GardenConfiguration : IEntityTypeConfiguration<Garden>
{
    public void Configure(EntityTypeBuilder<Garden> builder)
    {
        builder.ToTable("gardens");

        builder.HasKey(x => x.GardenId);

        builder.Property(x => x.GardenName)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(x => x.TotalSurfaceArea)
            .IsRequired()
            .HasPrecision(18, 2);

        builder.Property(x => x.LocationDescription)
            .IsRequired()
            .HasMaxLength(500);

        builder.Property(x => x.Latitude)
            .HasPrecision(9, 6)
            .IsRequired(false);

        builder.Property(x => x.Longitude)
            .HasPrecision(9, 6)
            .IsRequired(false);

        builder.Property(x => x.TargetHumidityLevel)
            .IsRequired();

        builder.Property(x => x.CreatedAtUtc)
            .IsRequired();

        builder.Property(x => x.DeletedAtUtc)
            .IsRequired(false);

        builder.HasQueryFilter(x => x.DeletedAtUtc == null);

        builder.HasOne<User>()
            .WithMany()
            .HasForeignKey(x => x.UserId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}