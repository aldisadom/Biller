namespace Application.Interfaces;

/// <summary>
/// Tells which characters of a text the invoice PDF font can't draw
/// </summary>
public interface IDocumentTextChecker
{
    /// <summary>
    /// The characters of <paramref name="text"/> the font can't draw, each once, in the order they
    /// first appear. A character outside the basic plane (an emoji) is one entry of two chars.
    /// Empty for null, empty and fully drawable text.
    /// </summary>
    IReadOnlyList<string> MissingCharacters(string? text);
}
