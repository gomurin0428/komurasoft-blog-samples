using System.Diagnostics;
using KomuraSoft.CpuSettings;
using Xunit;

namespace KomuraSoft.CpuSettings.Tests;

public class ProcessorAffinityHelperTests
{
    [Fact]
    public void CreateMask_FirstFourCpus_Returns0xF()
    {
        // 記事 3 章の「0xF は 2進数で 1111。論理プロセッサ 0〜3 を許可」を確認する
        nint mask = ProcessorAffinityHelper.CreateMask(0, 1, 2, 3);

        Assert.Equal((nint)0xF, mask);
    }

    [Fact]
    public void CreateMask_SingleCpu_SetsOnlyThatBit()
    {
        Assert.Equal((nint)0b0100, ProcessorAffinityHelper.CreateMask(2));
    }

    [Fact]
    public void CreateMask_DuplicateIndices_SetBitOnce()
    {
        Assert.Equal((nint)0b0010, ProcessorAffinityHelper.CreateMask(1, 1, 1));
    }

    [Fact]
    public void CreateMask_Empty_Throws()
    {
        Assert.Throws<ArgumentException>(
            () => ProcessorAffinityHelper.CreateMask());
    }

    [Fact]
    public void CreateMask_NegativeIndex_Throws()
    {
        Assert.Throws<ArgumentOutOfRangeException>(
            () => ProcessorAffinityHelper.CreateMask(-1));
    }

    [Fact]
    public void CreateMask_IndexBeyondPointerWidth_Throws()
    {
        int invalidIndex = IntPtr.Size * 8; // 64bit プロセスなら 64

        Assert.Throws<ArgumentOutOfRangeException>(
            () => ProcessorAffinityHelper.CreateMask(invalidIndex));
    }

    [Fact]
    public void GetAllowedCpus_0xF_ReturnsFirstFourCpus()
    {
        IReadOnlyList<int> allowed = ProcessorAffinityHelper.GetAllowedCpus(0xF);

        Assert.Equal(new[] { 0, 1, 2, 3 }, allowed);
    }

    [Fact]
    public void GetAllowedCpus_Zero_ReturnsEmpty()
    {
        Assert.Empty(ProcessorAffinityHelper.GetAllowedCpus(0));
    }

    [Fact]
    public void CreateMask_GetAllowedCpus_RoundTrip()
    {
        int[] cpus = { 0, 2, 3 };

        nint mask = ProcessorAffinityHelper.CreateMask(cpus);
        IReadOnlyList<int> allowed = ProcessorAffinityHelper.GetAllowedCpus(mask);

        Assert.Equal(cpus, allowed);
    }

    [Fact]
    public void SetAffinityMask_Zero_Throws()
    {
        if (!ProcessorAffinityHelper.IsSupported)
        {
            return; // Windows / Linux 以外ではアフィニティ操作自体が未サポート
        }

        using var process = Process.GetCurrentProcess();

        Assert.Throws<ArgumentOutOfRangeException>(
            () => ProcessorAffinityHelper.SetAffinityMask(process, 0));
    }

    [Fact]
    public void SetAffinityMask_NarrowAndRestore_OnCurrentProcess()
    {
        // Process.ProcessorAffinity は Windows / Linux でサポートされる（実行検証）
        if (!ProcessorAffinityHelper.IsSupported || Environment.ProcessorCount < 2)
        {
            return;
        }

        using var process = Process.GetCurrentProcess();
        nint original = ProcessorAffinityHelper.GetAffinityMask(process);

        try
        {
            nint narrowed = ProcessorAffinityHelper.CreateMask(0);
            ProcessorAffinityHelper.SetAffinityMask(process, narrowed);

            Assert.Equal(narrowed, ProcessorAffinityHelper.GetAffinityMask(process));
            Assert.Equal(new[] { 0 }, ProcessorAffinityHelper.GetAllowedCpus(narrowed));
        }
        finally
        {
            ProcessorAffinityHelper.SetAffinityMask(process, original);
        }

        Assert.Equal(original, ProcessorAffinityHelper.GetAffinityMask(process));
    }
}
