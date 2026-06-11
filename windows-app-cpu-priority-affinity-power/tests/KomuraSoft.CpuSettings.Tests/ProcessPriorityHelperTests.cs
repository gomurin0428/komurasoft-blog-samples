using System.Diagnostics;
using System.Runtime.InteropServices;
using KomuraSoft.CpuSettings;
using Xunit;

namespace KomuraSoft.CpuSettings.Tests;

public class ProcessPriorityHelperTests
{
    [Fact]
    public void GetPriorityClass_CurrentProcess_ReturnsDefinedValue()
    {
        if (!ProcessPriorityHelper.IsSupported)
        {
            return; // Windows / Linux 以外では PriorityClass が未サポート
        }

        using var process = Process.GetCurrentProcess();

        ProcessPriorityClass priorityClass =
            ProcessPriorityHelper.GetPriorityClass(process);

        Assert.True(
            Enum.IsDefined(priorityClass),
            $"Unexpected priority class: {priorityClass}");
    }

    [Fact]
    public void TemporaryPriorityScope_LowersThenRestores()
    {
        // 記事 12 章「一時的に上げて（変えて）、終わったら戻す」の実行検証。
        // Linux で優先度を元へ戻す（上げ直す）には root などの権限が必要なため、
        // 権限がない環境ではスキップする。
        if (!CanRoundTripPriority())
        {
            return;
        }

        using var process = Process.GetCurrentProcess();
        ProcessPriorityClass original =
            ProcessPriorityHelper.GetPriorityClass(process);

        using (var scope = new TemporaryPriorityScope(
            process,
            ProcessPriorityClass.BelowNormal))
        {
            Assert.Equal(original, scope.OriginalPriorityClass);

            process.Refresh();
            Assert.Equal(
                ProcessPriorityClass.BelowNormal,
                ProcessPriorityHelper.GetPriorityClass(process));
        }

        process.Refresh();
        Assert.Equal(original, ProcessPriorityHelper.GetPriorityClass(process));
    }

    [Fact]
    public void TemporaryPriorityScope_DisposeTwice_DoesNotThrow()
    {
        if (!CanRoundTripPriority())
        {
            return;
        }

        using var process = Process.GetCurrentProcess();

        var scope = new TemporaryPriorityScope(
            process,
            ProcessPriorityClass.BelowNormal);

        scope.Dispose();
        scope.Dispose(); // 2 回呼んでも安全
    }

    private static bool CanRoundTripPriority()
    {
        if (OperatingSystem.IsWindows())
        {
            return true;
        }

        // Linux では優先度を上げる方向の変更に権限が必要（nice 値を下げる操作）
        return OperatingSystem.IsLinux() && geteuid() == 0;
    }

    [DllImport("libc")]
    private static extern uint geteuid();
}
