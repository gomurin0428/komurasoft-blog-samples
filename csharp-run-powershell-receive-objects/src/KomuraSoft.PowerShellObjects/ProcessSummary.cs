namespace KomuraSoft.PowerShellObjects;

/// <summary>
/// PowerShell の結果（PSObject）から変換して使う C# 側の型です（記事 6 章）。
/// PSObject を境界で扱い、アプリ内部ではこのような通常の C# の型に変換します。
/// </summary>
public sealed record ProcessSummary(
    string Name,
    int Id,
    double? Cpu,
    long WorkingSet);
