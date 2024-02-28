namespace Contracts.Responses;

public record ErrorResponse
{
    public string ErrorMessage { get; set; } = string.Empty;
}
