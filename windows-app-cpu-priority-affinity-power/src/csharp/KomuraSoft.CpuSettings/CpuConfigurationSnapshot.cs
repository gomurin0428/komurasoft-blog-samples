using System.Diagnostics;
using System.Runtime.InteropServices;

namespace KomuraSoft.CpuSettings;

/// <summary>
/// プロセスの CPU 関連設定のスナップショットです。
/// 記事 14 章「実装より先に、ログを設計する」の通り、顧客環境で「たまに遅い」を切り分けるために、
/// 優先度・アフィニティ・論理プロセッサ数などを診断ログへ残せる形にまとめます。
/// </summary>
public sealed record CpuConfigurationSnapshot
{
    public required int ProcessId { get; init; }

    public required string ProcessName { get; init; }

    public required string OSDescription { get; init; }

    /// <summary>論理プロセッサ数。</summary>
    public required int ProcessorCount { get; init; }

    /// <summary>プロセス優先度クラス。サポートされない OS では null。</summary>
    public ProcessPriorityClass? PriorityClass { get; init; }

    /// <summary>アフィニティマスク。サポートされない OS では null。</summary>
    public nint? AffinityMask { get; init; }

    /// <summary>アフィニティマスクが許可する論理プロセッサ番号。サポートされない OS では null。</summary>
    public IReadOnlyList<int>? AllowedCpus { get; init; }

    /// <summary>指定したプロセスのスナップショットを取得します。</summary>
    public static CpuConfigurationSnapshot Capture(Process process)
    {
        ArgumentNullException.ThrowIfNull(process);

        ProcessPriorityClass? priorityClass = null;
        nint? affinityMask = null;
        IReadOnlyList<int>? allowedCpus = null;

        if (ProcessPriorityHelper.IsSupported)
        {
            priorityClass = ProcessPriorityHelper.GetPriorityClass(process);
        }

        if (ProcessorAffinityHelper.IsSupported)
        {
            nint mask = ProcessorAffinityHelper.GetAffinityMask(process);
            affinityMask = mask;
            allowedCpus = ProcessorAffinityHelper.GetAllowedCpus(mask);
        }

        return new CpuConfigurationSnapshot
        {
            ProcessId = process.Id,
            ProcessName = process.ProcessName,
            OSDescription = RuntimeInformation.OSDescription,
            ProcessorCount = Environment.ProcessorCount,
            PriorityClass = priorityClass,
            AffinityMask = affinityMask,
            AllowedCpus = allowedCpus,
        };
    }
}
