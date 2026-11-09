using Application.Models.Invoice.Documents;
using Domain.IOptions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using QuestPDF.Drawing;
using QuestPDF.Infrastructure;

namespace Infrastructure;

public static class DependencyInjectionInvoiceGeneration
{
    public static IServiceCollection AddPdfGenerator(this IServiceCollection services, IOptions<FontSettings> fontSettings)
        => services.AddPdfGenerator(fontSettings, InvoiceDocumentFont.Family);

    /// <summary>
    /// AddPdfGenerator with the font family that must be registered afterwards. Tests pass a family
    /// nobody registers: QuestPDF's font registry is process-wide, so the documents' family may
    /// already be there from another test.
    /// </summary>
    internal static IServiceCollection AddPdfGenerator(this IServiceCollection services, IOptions<FontSettings> fontSettings, string requiredFamily)
    {
        QuestPDF.Settings.License = LicenseType.Community;
        // Strict: a PDF fails instead of quietly substituting. Only the registered fonts are used (no
        // system fonts, so the output doesn't depend on the machine), and a missing font family or a
        // character the font can't draw throws. Set explicitly so a change of QuestPDF's defaults can't
        // relax it. Layout error messages leave out document text, because they end up in the logs.
        QuestPDF.Settings.UseSystemFonts = false;
        QuestPDF.Settings.ThrowOnMissingFontFamilies = true;
        QuestPDF.Settings.ThrowOnMissingTextGlyphs = true;
        QuestPDF.Settings.EnableDetailedLayoutErrors = false;

        if (fontSettings == null)
            throw new ArgumentNullException(nameof(fontSettings), "Font settings cannot be null");

        if (fontSettings.Value == null || fontSettings.Value.Fonts == null || fontSettings.Value.Fonts.Length == 0)
            throw new ArgumentException("Font settings must contain at least one font");

        foreach (var font in fontSettings.Value.Fonts)
        {
            if (string.IsNullOrEmpty(font.Path) || string.IsNullOrEmpty(font.Name))
                throw new ArgumentException("Font path or name cannot be null or empty");

            if (!File.Exists(font.Path))
                throw new FileNotFoundException($"Font {font.Name} file not found at path: {font.Path}");
            var aa = File.ReadAllBytes(font.Path);
            // Registered under the family name stored in the file (Calibri); QuestPDF matches family
            // names case-insensitively, so the documents' "calibri" finds it.
            using FileStream fontStream = File.OpenRead(font.Path);
            FontManager.RegisterFontFromStream(fontStream);
        }

        // Fail at startup, not on every PDF, when no configured file holds the documents' font family.
        EnsureFontFamilyRegistered(FontManager.GetRegisteredFonts().Select(f => f.FamilyName), requiredFamily);

        return services;
    }

    /// <summary>
    /// Throws when <paramref name="family"/> isn't among the registered font families (compared ignoring case, as QuestPDF does).
    /// </summary>
    public static void EnsureFontFamilyRegistered(IEnumerable<string> registeredFamilies, string family)
    {
        if (!registeredFamilies.Any(f => string.Equals(f, family, StringComparison.OrdinalIgnoreCase)))
            throw new ArgumentException($"No configured font file contains the font family {family} that the documents use");
    }
}
