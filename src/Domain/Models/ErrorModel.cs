namespace Domain.Models;

public class ErrorModel
{
    public int StatusCode { get; set; }
    public Exception Exception { get; set; }
    public string Message { get; set; }
    public string ExtendedMessage { get; set; }

    public ErrorModel(string message, string extendedMessage, int statusCode, Exception e)
    {
        Message = message;
        ExtendedMessage = extendedMessage;
        Exception = e;
        StatusCode = statusCode;
    }
}
