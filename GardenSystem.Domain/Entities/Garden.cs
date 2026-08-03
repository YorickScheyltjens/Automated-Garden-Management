namespace GardenSystem.Domain.Entities;

public sealed class Garden
{
    public Guid GardenId { get; set; }
    public Guid UserId { get; set; }
    public string GardenName { get; set; } = string.Empty;
    public decimal TotalSurfaceArea { get; set; }
    public string LocationDescription { get; set; } = string.Empty;
    public decimal? Latitude { get; set; }
    public decimal? Longitude { get; set; }
    public DateTime CreatedAtUtc { get; set; }
    public DateTime? DeletedAtUtc { get; set; }
}