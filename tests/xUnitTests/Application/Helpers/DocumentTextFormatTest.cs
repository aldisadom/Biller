using Application.Helpers;
using AwesomeAssertions;

namespace xUnitTests.Application.Helpers;

public class DocumentTextFormatTest
{
    [Theory]
    [InlineData(new[] { "!" }, "! (U+0021)", Label = "a visible character is shown with its code point")]
    [InlineData(new[] { "🐉", "鐵" }, "🐉 (U+1F409), 鐵 (U+9435)", Label = "several characters are separated by commas")]
    [InlineData(new[] { "\t" }, "U+0009", Label = "a control character is shown as its code point only")]
    [InlineData(new[] { "‍" }, "U+200D", Label = "an invisible format character is shown as its code point only")]
    [InlineData(new[] { " " }, "U+00A0", Label = "a space character is shown as its code point only")]
    [InlineData(new[] { "1", "2", "3", "4", "5" }, "1 (U+0031), 2 (U+0032), 3 (U+0033), 4 (U+0034), 5 (U+0035)", Label = "five characters are all listed")]
    [InlineData(new[] { "1", "2", "3", "4", "5", "6", "7" }, "1 (U+0031), 2 (U+0032), 3 (U+0033), 4 (U+0034), 5 (U+0035) and 2 more", Label = "characters after the fifth are counted, not listed")]
    public void Describe_GivenMissingCharacters_ReturnsExpected(string[] missingCharacters, string expected)
    {
        //Act
        string result = DocumentTextFormat.Describe(missingCharacters);

        //Assert
        result.Should().Be(expected);
    }

    // Built at run time: an attribute argument can't hold half of a surrogate pair
    [Fact]
    public void Describe_GivenHalfOfASurrogatePair_ReturnsItsCodePointOnly()
    {
        //Arrange
        string half = ((char)0xD83D).ToString();

        //Act
        string result = DocumentTextFormat.Describe([half]);

        //Assert
        result.Should().Be("U+D83D");
    }
}
