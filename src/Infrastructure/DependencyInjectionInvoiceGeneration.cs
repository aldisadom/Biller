using Domain.IOptions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using QuestPDF.Drawing;
using QuestPDF.Infrastructure;

namespace Infrastructure;

public static class DependencyInjectionInvoiceGeneration
{
    public static IServiceCollection AddPdfGenerator(this IServiceCollection services, IOptions<FontSettings> fontSettings)
    {
        QuestPDF.Settings.License = LicenseType.Community;
        // QuestPDF 2026.9 changed these defaults. Keep the behaviour from before it: system fonts as a
        // fallback, a placeholder instead of an exception for a missing glyph, and no document text
        // in layout exception messages (they end up in the logs).
        QuestPDF.Settings.UseSystemFonts = true;
        QuestPDF.Settings.ThrowOnMissingTextGlyphs = false;
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

        return services;
    }
}
