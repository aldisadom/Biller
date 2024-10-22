namespace Contracts.Responses;

public record ErrorResponse
{
    public string Message { get; set; } = string.Empty;
    public string ExtendedMessage { get; set; } = string.Empty;
}
