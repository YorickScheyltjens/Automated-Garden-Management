using GardenSystem.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GardenSystem.Infrastructure.Persistence.Configurations;

public sealed class IrrigationEventConfiguration : IEntityTypeConfiguration<IrrigationEvent>
{
    public void Configure(EntityTypeBuilder<IrrigationEvent> builder)
    {
        builder.ToTable("irrigation_events");

        builder.HasKey(x => x.IrrigationEventId);

        builder.Property(x => x.StartTimeUtc)
            .IsRequired();

        builder.Property(x => x.EndTimeUtc)
            .IsRequired(false);

        builder.Property(x => x.HumidityBefore)
            .IsRequired()
            .HasPrecision(18, 2);

        builder.Property(x => x.HumidityAfter)
            .IsRequired(false)
            .HasPrecision(18, 2);

        builder.HasIndex(x => new { x.PlantId, x.StartTimeUtc });

        builder.HasOne<Plant>()
            .WithMany()
            .HasForeignKey(x => x.PlantId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
