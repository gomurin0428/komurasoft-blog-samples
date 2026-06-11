using System.Diagnostics;
using System.Runtime.Versioning;

namespace KomuraSoft.CpuSettings;

/// <summary>
/// プロセッサアフィニティ（記事 3 章）の取得・設定と、アフィニティマスクの組み立て・分解を行います。
/// アフィニティは「どのCPUで実行してよいか」をスケジューラへ強めに伝える設定です。
/// <see cref="Process.ProcessorAffinity"/> は Windows と Linux でサポートされます。
/// 記事 3 章の通り、CPU 番号の決め打ちは危険であり、Processor Group（64 論理プロセッサ超）や
/// P コア / E コア混在環境では CPU Sets も含めて検討してください。
/// </summary>
public static class ProcessorAffinityHelper
{
    /// <summary>この OS でアフィニティの取得・設定がサポートされているかどうか。</summary>
    [SupportedOSPlatformGuard("windows")]
    [SupportedOSPlatformGuard("linux")]
    public static bool IsSupported =>
        OperatingSystem.IsWindows() || OperatingSystem.IsLinux();

    /// <summary>
    /// 論理プロセッサ番号の集合からアフィニティマスクを組み立てます。
    /// 例: <c>CreateMask(0, 1, 2, 3)</c> は記事の <c>0xF</c>（2進数で 1111）になります。
    /// </summary>
    public static nint CreateMask(params int[] cpuIndices)
    {
        ArgumentNullException.ThrowIfNull(cpuIndices);

        if (cpuIndices.Length == 0)
        {
            throw new ArgumentException(
                "At least one logical processor index is required.",
                nameof(cpuIndices));
        }

        int maxIndex = IntPtr.Size * 8 - 1; // 64bit プロセスなら 63

        ulong mask = 0;

        foreach (int index in cpuIndices)
        {
            if (index < 0 || index > maxIndex)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(cpuIndices),
                    index,
                    $"Logical processor index must be between 0 and {maxIndex}.");
            }

            mask |= 1UL << index;
        }

        return unchecked((nint)mask);
    }

    /// <summary>
    /// アフィニティマスクを論理プロセッサ番号の一覧に分解します。
    /// 例: <c>0xF</c> は <c>[0, 1, 2, 3]</c> になります。
    /// </summary>
    public static IReadOnlyList<int> GetAllowedCpus(nint mask)
    {
        ulong bits = unchecked((ulong)mask);
        var allowed = new List<int>();

        for (int index = 0; index < IntPtr.Size * 8; index++)
        {
            if ((bits & (1UL << index)) != 0)
            {
                allowed.Add(index);
            }
        }

        return allowed;
    }

    /// <summary>プロセスのアフィニティマスクを取得します。</summary>
    public static nint GetAffinityMask(Process process)
    {
        ArgumentNullException.ThrowIfNull(process);

        if (!IsSupported)
        {
            throw CreateNotSupported();
        }

        return process.ProcessorAffinity;
    }

    /// <summary>
    /// プロセスのアフィニティマスクを設定します（記事のコードの
    /// <c>$p.ProcessorAffinity = [IntPtr]0xF</c> に相当）。
    /// 記事 3 章の通り、これは検証用の強い制約であり、最初に触る設定ではありません。
    /// </summary>
    public static void SetAffinityMask(Process process, nint mask)
    {
        ArgumentNullException.ThrowIfNull(process);

        if (!IsSupported)
        {
            throw CreateNotSupported();
        }

        if (mask == 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(mask),
                "Affinity mask must have at least one bit set.");
        }

        process.ProcessorAffinity = mask;
    }

    private static PlatformNotSupportedException CreateNotSupported() =>
        new("Process.ProcessorAffinity is supported only on Windows and Linux.");
}
