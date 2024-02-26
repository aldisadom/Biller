namespace Domain.Exceptions;

public class ClientAPIException : Exception
{
    public ClientAPIException()
    {
    }

    public ClientAPIException(string message) : base(message)
    {
    }

    public ClientAPIException(string message, Exception innerException) : base(message, innerException)
    {
    }
}