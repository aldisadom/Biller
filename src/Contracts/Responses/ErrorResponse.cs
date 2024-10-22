namespace Contracts.Responses;

public record ErrorResponse
{
    public string Message { get; set; } = string.Empty;
}
