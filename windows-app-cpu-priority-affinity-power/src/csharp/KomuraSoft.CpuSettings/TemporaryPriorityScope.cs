using System.Diagnostics;

namespace KomuraSoft.CpuSettings;

/// <summary>
/// 優先度クラスを一時的に変更し、Dispose で元に戻すスコープです。
/// 記事 12 章「優先度は一時的に使う」（重要な処理の直前に上げて、終わったら戻す）の実装例です。
/// </summary>
public sealed class TemporaryPriorityScope : IDisposable
{
    private readonly Process _process;
    private readonly ProcessPriorityClass _original;
    private bool _disposed;

    /// <summary>変更前の優先度クラス。</summary>
    public ProcessPriorityClass OriginalPriorityClass => _original;

    public TemporaryPriorityScope(
        Process process,
        ProcessPriorityClass temporaryPriorityClass)
    {
        ArgumentNullException.ThrowIfNull(process);

        _process = process;
        _original = ProcessPriorityHelper.GetPriorityClass(process);

        ProcessPriorityHelper.SetPriorityClass(process, temporaryPriorityClass);
    }

    /// <summary>優先度クラスを変更前の値へ戻します。</summary>
    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;
        _process.Refresh();
        ProcessPriorityHelper.SetPriorityClass(_process, _original);
    }
}
