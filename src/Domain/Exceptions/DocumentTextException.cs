namespace Domain.Exceptions;

/// <summary>
/// A text that goes on an invoice or act PDF has a character the PDF font can't draw.
/// The message names the field and the characters, never the text itself: it is logged.
/// </summary>
public class DocumentTextException : Exception
{
    public DocumentTextException(string message) : base(message)
    {
    }
}
