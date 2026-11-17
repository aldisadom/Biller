using Application.Interfaces;
using Infrastructure;

namespace xUnitTests;

/// <summary>
/// The repo's font files, found from the test dll (tests/xUnitTests/bin/&lt;config&gt;/&lt;tfm&gt;), not the
/// working directory. They aren't copied next to the dll: QuestPDF would register them from there by
/// itself, and the tests could no longer tell whether AddPdfGenerator registered them.
/// </summary>
internal static class RepoFonts
{
    public static readonly string CalibriPath =
        Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "..", "Fonts", "calibri.ttf"));

    /// <summary>
    /// The checker the app builds from Calibri, for validators and services that need one
    /// </summary>
    public static readonly IDocumentTextChecker CalibriChecker =
        new DocumentTextChecker(FontCharacterReader.Read(File.ReadAllBytes(CalibriPath), CalibriPath));
}

/// <summary>
/// The test classes that set or rely on QuestPDF's process-wide settings and fonts share this
/// collection, so they don't run in parallel with each other.
/// </summary>
internal static class QuestPdfCollection
{
    public const string Name = "QuestPDF settings";
}
