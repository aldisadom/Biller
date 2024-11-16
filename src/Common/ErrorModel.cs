using System.Net;

namespace Common;

public class ErrorModel
{
    public HttpStatusCode StatusCode { get; set; }
    public string Message { get; set; }
    public string ExtendedMessage { get; set; }

    public ErrorModel()
    {
        Message = string.Empty;
        ExtendedMessage = string.Empty;
    }

    public ErrorModel(string message, string extendedMessage, HttpStatusCode statusCode)
    {
        Message = message;
        ExtendedMessage = extendedMessage;
        StatusCode = statusCode;
    }

    public override string ToString()
    {
        string errorMessage = $"Error status code: {StatusCode}";

        if (!string.IsNullOrEmpty(Message))
        {
            errorMessage += $", message: " + Message;
            if (!string.IsNullOrEmpty(Message))
                errorMessage += $", extended message: " + ExtendedMessage;
        }
        return errorMessage;
    }
}
