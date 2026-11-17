using AwesomeAssertions;
using Infrastructure;

namespace xUnitTests.Infrastructure;

public class DocumentTextCheckerTest
{
    // The font draws only the letters of "Winterfell" and the space
    private readonly DocumentTextChecker _checker = new("Winterfell ".Select(c => (int)c).ToHashSet());

    [Theory]
    [InlineData(null, Label = "null text has no missing characters")]
    [InlineData("", Label = "empty text has no missing characters")]
    [InlineData("Winter fell", Label = "text of drawable characters has no missing characters")]
    [InlineData("Winter\nfell\r\n", Label = "line breaks are not missing characters")]
    public void MissingCharacters_GivenDrawableText_ReturnsEmpty(string? text)
    {
        //Act
        IReadOnlyList<string> result = _checker.MissingCharacters(text);

        //Assert
        result.Should().BeEmpty();
    }

    [Theory]
    [InlineData("Winterfell!", new[] { "!" }, Label = "a character the font lacks is returned")]
    [InlineData("Wolves of Winterfell", new[] { "o", "v", "s" }, Label = "missing characters are returned once each, in the order they first appear")]
    [InlineData("Winter 🐉 fell", new[] { "🐉" }, Label = "an emoji is one character, not two halves")]
    [InlineData("Winter\tfell", new[] { "\t" }, Label = "a tab is not a line break and is returned")]
    public void MissingCharacters_GivenTextTheFontCannotDraw_ReturnsTheCharacters(string text, string[] expected)
    {
        //Act
        IReadOnlyList<string> result = _checker.MissingCharacters(text);

        //Assert
        result.Should().Equal(expected);
    }

    // Code points, not strings: the test runner drops the result of a test whose failure message
    // holds a null character, so a failing case would go unreported
    [Theory]
    [InlineData(0x00, Label = "a null character is missing although the font maps it")]
    [InlineData(0x1B, Label = "an escape character is missing although the font maps it")]
    [InlineData(0x85, Label = "a control character above ASCII is missing although the font maps it")]
    public void MissingCharacters_GivenControlCharacterTheFontMaps_ReturnsIt(int codePoint)
    {
        //Arrange
        DocumentTextChecker checker = new("Winterfell".Select(c => (int)c).Append(codePoint).ToHashSet());
        string text = "Winter" + (char)codePoint + "fell";

        //Act
        IReadOnlyList<string> result = checker.MissingCharacters(text);

        //Assert
        result.Select(character => (int)character[0]).Should().Equal(codePoint);
    }

    // Built at run time: an attribute argument can't hold half of a surrogate pair
    [Theory]
    [InlineData("Winter", "fell", Label = "half of a surrogate pair inside the text is returned")]
    [InlineData("Winterfell", "", Label = "half of a surrogate pair at the end of the text is returned")]
    public void MissingCharacters_GivenHalfOfASurrogatePair_ReturnsIt(string before, string after)
    {
        //Arrange
        string half = ((char)0xD83D).ToString();

        //Act
        IReadOnlyList<string> result = _checker.MissingCharacters(before + half + after);

        //Assert
        result.Should().Equal(half);
    }
}
