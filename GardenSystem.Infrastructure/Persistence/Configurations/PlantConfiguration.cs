using GardenSystem.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GardenSystem.Infrastructure.Persistence.Configurations;

public sealed class PlantConfiguration : IEntityTypeConfiguration<Plant>
{
    public void Configure(EntityTypeBuilder<Plant> builder)
    {
        builder.ToTable("plants");

        builder.HasKey(x => x.PlantId);

        builder.Property(x => x.PlantName)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(x => x.Species)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(x => x.PlantType)
            .IsRequired();

        builder.Property(x => x.PlantationDate)
            .IsRequired();

        builder.Property(x => x.SurfaceAreaRequired)
            .IsRequired()
            .HasPrecision(18, 2);

        builder.Property(x => x.IdealHumidityLevel)
            .IsRequired();

        builder.Property(x => x.CreatedAtUtc)
            .IsRequired();

        builder.Property(x => x.DeletedAtUtc)
            .IsRequired(false);

        builder.HasOne<Garden>()
            .WithMany()
            .HasForeignKey(x => x.GardenId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}