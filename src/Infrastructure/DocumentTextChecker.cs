using Application.Interfaces;

namespace Infrastructure;

/// <summary>
/// Checks text against the characters the invoice PDF font can draw. Line breaks always pass:
/// QuestPDF breaks the line there and draws nothing. Every other control character is missing,
/// whatever the font says: Calibri maps U+0000, which neither a PDF nor PostgreSQL can hold.
/// </summary>
public class DocumentTextChecker : IDocumentTextChecker
{
    private readonly IReadOnlySet<int> _drawableCodePoints;

    /// <summary>
    ///
    /// </summary>
    /// <param name="drawableCodePoints">the Unicode code points the font has a glyph for</param>
    public DocumentTextChecker(IReadOnlySet<int> drawableCodePoints)
    {
        _drawableCodePoints = drawableCodePoints;
    }

    public IReadOnlyList<string> MissingCharacters(string? text)
    {
        if (string.IsNullOrEmpty(text))
            return [];

        List<string> missing = [];
        HashSet<int> seen = [];

        for (int i = 0; i < text.Length; i++)
        {
            int codePoint = text[i];
            string character = text[i].ToString();

            // Half of a surrogate pair on its own stays a surrogate code point, which no font draws
            if (char.IsHighSurrogate(text[i]) && i + 1 < text.Length && char.IsLowSurrogate(text[i + 1]))
            {
                codePoint = char.ConvertToUtf32(text[i], text[i + 1]);
                character = text.Substring(i, 2);
                i++;
            }

            if (codePoint == '\n' || codePoint == '\r')
                continue;

            bool drawable = _drawableCodePoints.Contains(codePoint) && !(codePoint <= char.MaxValue && char.IsControl((char)codePoint));
            if (!drawable && seen.Add(codePoint))
                missing.Add(character);
        }

        return missing;
    }
}
