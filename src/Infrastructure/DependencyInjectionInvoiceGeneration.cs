using Microsoft.Extensions.DependencyInjection;
using QuestPDF.Drawing;
using QuestPDF.Infrastructure;

namespace Infrastructure;

public static class DependencyInjectionInvoiceGeneration
{
    public static IServiceCollection AddPdfGenerator(this IServiceCollection services)
    {
        QuestPDF.Settings.License = LicenseType.Community;

        var projectDirectory = AppContext.BaseDirectory;
        var fontPath = Path.Combine(projectDirectory, "Fonts", "calibri.ttf");

        if (!File.Exists(fontPath))        
            throw new FileNotFoundException($"Font file not found at path: {fontPath}");
        
        FontManager.RegisterFontWithCustomName("calibri", File.OpenRead(fontPath));

        return services;
    }
}
