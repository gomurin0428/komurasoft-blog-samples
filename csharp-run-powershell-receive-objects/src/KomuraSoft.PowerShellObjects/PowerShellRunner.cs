using System.Management.Automation;

namespace KomuraSoft.PowerShellObjects;

/// <summary>
/// PowerShell の出力ストリームとエラーストリームをまとめた実行結果です（記事 11 章）。
/// </summary>
public sealed record PowerShellRunResult(
    IReadOnlyList<PSObject> Output,
    IReadOnlyList<ErrorRecord> Errors);

/// <summary>
/// 何度も PowerShell を呼び出すアプリ向けの小さな実行ラッパーです（記事 11 章）。
/// 使う側はコマンドの組み立てだけに集中できます。
/// </summary>
public static class PowerShellRunner
{
    public static PowerShellRunResult Run(Action<PowerShell> build)
    {
        using PowerShell ps = PowerShell.Create();

        build(ps);

        List<PSObject> output;

        try
        {
            output = ps.Invoke().ToList();
        }
        catch (RuntimeException ex)
        {
            throw new InvalidOperationException($"PowerShell execution failed: {ex.Message}", ex);
        }

        return new PowerShellRunResult(
            Output: output,
            Errors: ps.Streams.Error.ToList());
    }
}
