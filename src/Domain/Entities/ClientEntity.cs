namespace Domain.Entities;

public record ClientEntity
{
    public DateTime Date { get; set; }
    public Dictionary<string, decimal> Rates { get; set; } = [];
}