using Application.Interfaces;
using Domain.IOptions;
using QuestPDF.Drawing;
using QuestPDF.Infrastructure;

namespace Infrastructure;

/// <summary>
/// The process-wide QuestPDF setup: licence, strict settings and the document fonts.
/// </summary>
public static class PdfGeneratorSetup
{
    private const string RequiredCharacters = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789";

    /// <summary>
    /// Checks the font settings and files, and only then changes QuestPDF's global settings and
    /// registers the fonts, so a bad argument leaves QuestPDF as it was.
    /// </summary>
    /// <param name="fontSettings">the font files to register</param>
    /// <param name="requiredFamily">the font family that must be registered afterwards</param>
    /// <returns>a checker for the characters every configured font file can draw</returns>
    public static IDocumentTextChecker Apply(FontSettings? fontSettings, string requiredFamily)
    {
        if (fontSettings == null || fontSettings.Fonts == null || fontSettings.Fonts.Length == 0)
            throw new ArgumentException("Font settings must contain at least one font");

        List<byte[]> fontFiles = [];
        HashSet<int>? drawableCodePoints = null;
        foreach (var font in fontSettings.Fonts)
        {
            if (string.IsNullOrEmpty(font.Path))
                throw new ArgumentException("Font path cannot be null or empty");

            if (!File.Exists(font.Path))
                throw new FileNotFoundException($"Font file not found at path: {font.Path}");

            byte[] fontFile = File.ReadAllBytes(font.Path);
            HashSet<int> codePoints = FontCharacterReader.Read(fontFile, font.Path);

            // A character counts only when every configured file has it: QuestPDF may draw a text
            // with any of them (regular, bold), and a text one of them can't draw fails the PDF.
            if (drawableCodePoints == null)
                drawableCodePoints = codePoints;
            else
                drawableCodePoints.IntersectWith(codePoints);

            fontFiles.Add(fontFile);
        }

        // Font files of different families (a symbol font next to Calibri) share few characters, and
        // every save would then be rejected. Fail at startup instead.
        if (!RequiredCharacters.All(character => drawableCodePoints!.Contains(character)))
            throw new ArgumentException("The configured font files don't all have the letters A-Z, a-z and the digits 0-9. Configure only font files of the documents' font family");

        QuestPDF.Settings.License = LicenseType.Community;
        // Strict: a PDF fails instead of quietly substituting. Only the registered fonts are used (no
        // system fonts, so the output doesn't depend on the machine), and a missing font family or a
        // character the font can't draw throws. Set explicitly so a change of QuestPDF's defaults can't
        // relax it. Layout error messages leave out document text, because they end up in the logs.
        QuestPDF.Settings.UseSystemFonts = false;
        QuestPDF.Settings.ThrowOnMissingFontFamilies = true;
        QuestPDF.Settings.ThrowOnMissingTextGlyphs = true;
        QuestPDF.Settings.EnableDetailedLayoutErrors = false;

        foreach (byte[] fontFile in fontFiles)
        {
            // Registered under the family name stored in the file (Calibri); QuestPDF matches family
            // names case-insensitively, so the documents' "calibri" finds it.
            using MemoryStream fontStream = new(fontFile);
            FontManager.RegisterFontFromStream(fontStream);
        }

        // Fail at startup, not on every PDF, when no configured file holds the documents' font family.
        DependencyInjectionInvoiceGeneration.EnsureFontFamilyRegistered(FontManager.GetRegisteredFonts().Select(f => f.FamilyName), requiredFamily);

        return new DocumentTextChecker(drawableCodePoints!);
    }
}
