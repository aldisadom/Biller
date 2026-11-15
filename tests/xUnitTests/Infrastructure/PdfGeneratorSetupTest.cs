using Application.Interfaces;
using Application.Models.Invoice.Documents;
using AwesomeAssertions;
using Domain.IOptions;
using Infrastructure;
using QuestPDF.Drawing.Exceptions;
using QuestPDF.Fluent;

namespace xUnitTests.Infrastructure;

[Collection(QuestPdfCollection.Name)]
public class PdfGeneratorSetupTest
{
    private static FontSettings CalibriSettings() => new() { Fonts = [new FontSetting { Path = RepoFonts.CalibriPath }] };

    private static byte[] RenderPdf(string text)
    {
        return Document.Create(container => container.Page(page =>
        {
            page.DefaultTextStyle(style => style.FontFamily(InvoiceDocumentFont.Family));
            page.Content().Text(text);
        })).GeneratePdf();
    }

    [Theory]
    [InlineData("Ąžuolų šėtra €5", new string[0], Label = "lithuanian text and the euro sign have no missing characters")]
    [InlineData("Dragon 🐉 鐵", new[] { "🐉", "鐵" }, Label = "an emoji and a chinese character are missing")]
    public void Apply_GivenCalibri_ReturnsCheckerForItsCharacters(string text, string[] expectedMissing)
    {
        //Act
        IDocumentTextChecker result = PdfGeneratorSetup.Apply(CalibriSettings(), InvoiceDocumentFont.Family);

        //Assert
        result.MissingCharacters(text).Should().Equal(expectedMissing);
    }

    // The rule may reject more than QuestPDF does, never less: whatever the checker lets through
    // must render, or a saved text would still fail its PDF.
    [Fact]
    public void Apply_GivenCalibri_EveryCharacterTheCheckerAllowsRenders()
    {
        //Arrange
        IDocumentTextChecker checker = PdfGeneratorSetup.Apply(CalibriSettings(), InvoiceDocumentFont.Family);

        IEnumerable<string> everyCharacter = Enumerable.Range(0, 0x110000)
            .Where(codePoint => codePoint is < 0xD800 or > 0xDFFF)
            .Select(char.ConvertFromUtf32);
        string allowed = string.Concat(everyCharacter.Where(character => checker.MissingCharacters(character).Count == 0));

        //Act
        byte[] pdf = RenderPdf(allowed);
        // The same text with one character Calibri lacks at its very end: it throws only when
        // QuestPDF read the text to the end, so the render above checked every character
        Action renderWithMissingCharacterLast = () => RenderPdf(allowed + "🐉");

        //Assert
        allowed.Length.Should().BeGreaterThan(500, "Calibri has well over 500 characters");
        allowed.Contains('\0').Should().BeFalse("a null character would end the text for a native library");
        pdf.Should().NotBeEmpty();
        renderWithMissingCharacterLast.Should().Throw<DocumentDrawingException>()
            .WithMessage("*Missing glyphs*");
    }

    [Fact]
    public void Apply_GivenFontFilesWithoutCommonLetters_ThrowsAndLeavesQuestPdfSettingsUnchanged()
    {
        //Arrange
        PdfGeneratorSetup.Apply(CalibriSettings(), InvoiceDocumentFont.Family);
        // A second "font" that has only three dragon-range characters, like a symbol font next to Calibri
        string symbolFont = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid()}.ttf");
        File.WriteAllBytes(symbolFont, FontCharacterReaderTest.FontWithSubtables((3, 10, FontCharacterReaderTest.Format12(0x1F408, 0x1F40A, 1))));
        QuestPDF.Settings.EnableDetailedLayoutErrors = true;

        try
        {
            //Act
            Action act = () => PdfGeneratorSetup.Apply(new FontSettings
            {
                Fonts = [new FontSetting { Path = RepoFonts.CalibriPath }, new FontSetting { Path = symbolFont }]
            }, InvoiceDocumentFont.Family);

            //Assert
            act.Should().Throw<ArgumentException>()
                .WithMessage("The configured font files don't all have the letters A-Z, a-z and the digits 0-9. Configure only font files of the documents' font family");
            QuestPDF.Settings.EnableDetailedLayoutErrors.Should().BeTrue("a bad argument must not change the global settings");
        }
        finally
        {
            QuestPDF.Settings.EnableDetailedLayoutErrors = false;
            File.Delete(symbolFont);
        }
    }

    [Fact]
    public void Apply_GivenFileThatIsNotAFont_ThrowsAndLeavesQuestPdfSettingsUnchanged()
    {
        //Arrange
        PdfGeneratorSetup.Apply(CalibriSettings(), InvoiceDocumentFont.Family);
        string notAFont = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid()}.ttf");
        File.WriteAllText(notAFont, "Winter is coming");
        // Apply sets this to false. Still true afterwards: the arguments were checked first.
        QuestPDF.Settings.EnableDetailedLayoutErrors = true;

        try
        {
            //Act
            Action act = () => PdfGeneratorSetup.Apply(new FontSettings { Fonts = [new FontSetting { Path = notAFont }] }, InvoiceDocumentFont.Family);

            //Assert
            act.Should().Throw<ArgumentException>()
                .WithMessage($"Font file is damaged or not a font: {notAFont}");
            QuestPDF.Settings.EnableDetailedLayoutErrors.Should().BeTrue("a bad argument must not change the global settings");
        }
        finally
        {
            QuestPDF.Settings.EnableDetailedLayoutErrors = false;
            File.Delete(notAFont);
        }
    }
}
