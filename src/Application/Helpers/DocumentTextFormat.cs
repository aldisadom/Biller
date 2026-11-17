using System.Globalization;

namespace Application.Helpers;

/// <summary>
/// Writes the characters a PDF can't show for an error message
/// </summary>
public static class DocumentTextFormat
{
    public const int MaxListed = 5;

    /// <summary>
    /// "🐉 (U+1F409), 鐵 (U+9435)": at most <see cref="MaxListed"/> characters, then "and N more".
    /// A character that has no visible form (a control character, half of a surrogate pair) is
    /// written as its code point only.
    /// </summary>
    public static string Describe(IReadOnlyList<string> missingCharacters)
    {
        IEnumerable<string> listed = missingCharacters.Take(MaxListed).Select(DescribeOne);
        string text = string.Join(", ", listed);

        int more = missingCharacters.Count - MaxListed;
        return more > 0 ? $"{text} and {more} more" : text;
    }

    private static string DescribeOne(string character)
    {
        bool isPair = character.Length == 2 && char.IsSurrogatePair(character[0], character[1]);
        int codePoint = isPair ? char.ConvertToUtf32(character[0], character[1]) : character[0];
        string code = $"U+{codePoint:X4}";

        if (!isPair && char.IsSurrogate(character[0]))
            return code;

        return CharUnicodeInfo.GetUnicodeCategory(codePoint) switch
        {
            UnicodeCategory.Control or UnicodeCategory.Format or UnicodeCategory.OtherNotAssigned
                or UnicodeCategory.PrivateUse or UnicodeCategory.LineSeparator
                or UnicodeCategory.ParagraphSeparator or UnicodeCategory.SpaceSeparator => code,
            _ => $"{character} ({code})"
        };
    }
}
