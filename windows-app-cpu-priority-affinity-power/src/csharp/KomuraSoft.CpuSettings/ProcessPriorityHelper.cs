using System.Diagnostics;
using System.Runtime.Versioning;

namespace KomuraSoft.CpuSettings;

/// <summary>
/// プロセス優先度クラス（記事 2 章）の取得・設定を行います。
/// 優先度は「いつ実行されやすいか」に関わる設定であり、CPU を速くする設定ではありません。
/// <see cref="Process.PriorityClass"/> は Windows と Linux でサポートされます
/// （Linux では nice 値にマップされ、Normal より上げるには権限が必要です）。
/// </summary>
public static class ProcessPriorityHelper
{
    /// <summary>この OS で優先度クラスの取得・設定がサポートされているかどうか。</summary>
    [SupportedOSPlatformGuard("windows")]
    [SupportedOSPlatformGuard("linux")]
    public static bool IsSupported =>
        OperatingSystem.IsWindows() || OperatingSystem.IsLinux();

    /// <summary>プロセスの優先度クラスを取得します。</summary>
    public static ProcessPriorityClass GetPriorityClass(Process process)
    {
        ArgumentNullException.ThrowIfNull(process);

        if (!IsSupported)
        {
            throw CreateNotSupported();
        }

        return process.PriorityClass;
    }

    /// <summary>
    /// プロセスの優先度クラスを設定します（記事のコードの
    /// <c>process.PriorityClass = ProcessPriorityClass.AboveNormal;</c> に相当）。
    /// 記事 2 章の通り、High / RealTime の常用は避けてください。
    /// </summary>
    public static void SetPriorityClass(
        Process process,
        ProcessPriorityClass priorityClass)
    {
        ArgumentNullException.ThrowIfNull(process);

        if (!IsSupported)
        {
            throw CreateNotSupported();
        }

        process.PriorityClass = priorityClass;
    }

    private static PlatformNotSupportedException CreateNotSupported() =>
        new("Process.PriorityClass is supported only on Windows and Linux.");
}
