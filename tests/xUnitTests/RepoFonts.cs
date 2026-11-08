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
}
