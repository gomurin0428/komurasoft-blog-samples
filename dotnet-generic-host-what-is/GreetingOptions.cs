namespace GenericHostSample;

/// <summary>
/// appsettings.json の "Greeting" セクションをバインドするオプションクラス。
/// 構成(IConfiguration)を型付きで受け取るオプションパターンの例。
/// </summary>
public sealed class GreetingOptions
{
    public const string SectionName = "Greeting";

    public string Message { get; set; } = "Hello";

    public int IntervalSeconds { get; set; } = 5;
}
