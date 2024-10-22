namespace Domain.Models;

public class ErrorModel
{
    public int StatusCode { get; set; }
    public string ExceptionMessage { get; set; }
    public string Message { get; set; }
    public string ExtendedMessage { get; set; }
    public string StackTrace { get; set; }

    public ErrorModel(string message, string extendedMessage, string exceptionMessage, int statusCode, Exception e)
    {
        Message = message;
        ExtendedMessage = extendedMessage;
        ExceptionMessage = exceptionMessage;
        StatusCode = statusCode;
        StackTrace = e.StackTrace ?? string.Empty;
    }
}
