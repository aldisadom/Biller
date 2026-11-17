using Application.Interfaces;
using Application.Models.Invoice.Documents;
using Domain.IOptions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

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
        if (fontSettings == null)
            throw new ArgumentNullException(nameof(fontSettings), "Font settings cannot be null");

        // The QuestPDF settings and fonts are process-wide, so they are set up here, once at startup,
        // and not when a service is first resolved.
        IDocumentTextChecker documentTextChecker = PdfGeneratorSetup.Apply(fontSettings.Value, requiredFamily);

        return services.AddSingleton(documentTextChecker);
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
