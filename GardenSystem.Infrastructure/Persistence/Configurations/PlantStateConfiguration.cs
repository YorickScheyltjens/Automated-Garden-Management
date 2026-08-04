using GardenSystem.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GardenSystem.Infrastructure.Persistence.Configurations;

public sealed class PlantStateConfiguration : IEntityTypeConfiguration<PlantState>
{
    public void Configure(EntityTypeBuilder<PlantState> builder)
    {
        builder.ToTable("plant_states");

        builder.HasKey(x => x.PlantId);

        builder.Property(x => x.CurrentHumidityLevel)
            .IsRequired()
            .HasPrecision(18, 2);

        builder.Property(x => x.LastIrrigationStartTime)
            .IsRequired(false);

        builder.Property(x => x.LastIrrigationEndTime)
            .IsRequired(false);

        builder.Property(x => x.IsCurrentlyIrrigating)
            .IsRequired();

        builder.Property(x => x.UpdatedAtUtc)
            .IsRequired();

        builder.HasOne<Plant>()
            .WithOne()
            .HasForeignKey<PlantState>(x => x.PlantId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
