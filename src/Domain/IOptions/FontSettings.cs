namespace Domain.IOptions;

public class FontSettings
{
    public FontSetting[] Fonts { get; set; } = [];
}

public class FontSetting
{
    public string Path { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
}
