namespace Domain.Entities;

public record SeriesEntity
{
    public Guid Id { get; set; }
    public Guid SellerId { get; set; }
    public string Name { get; set; } = string.Empty;
    public int NextNumber { get; set; }
}
