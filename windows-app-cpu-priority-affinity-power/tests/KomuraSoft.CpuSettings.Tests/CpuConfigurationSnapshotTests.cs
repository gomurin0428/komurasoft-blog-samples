using System.Diagnostics;
using KomuraSoft.CpuSettings;
using Xunit;

namespace KomuraSoft.CpuSettings.Tests;

public class CpuConfigurationSnapshotTests
{
    [Fact]
    public void Capture_CurrentProcess_PopulatesBasicInfo()
    {
        using var process = Process.GetCurrentProcess();

        CpuConfigurationSnapshot snapshot =
            CpuConfigurationSnapshot.Capture(process);

        Assert.Equal(process.Id, snapshot.ProcessId);
        Assert.False(string.IsNullOrWhiteSpace(snapshot.ProcessName));
        Assert.False(string.IsNullOrWhiteSpace(snapshot.OSDescription));
        Assert.Equal(Environment.ProcessorCount, snapshot.ProcessorCount);
    }

    [Fact]
    public void Capture_OnSupportedPlatform_IncludesPriorityAndAffinity()
    {
        if (!ProcessPriorityHelper.IsSupported ||
            !ProcessorAffinityHelper.IsSupported)
        {
            return; // Windows / Linux 以外では null になる仕様
        }

        using var process = Process.GetCurrentProcess();

        CpuConfigurationSnapshot snapshot =
            CpuConfigurationSnapshot.Capture(process);

        Assert.NotNull(snapshot.PriorityClass);
        Assert.NotNull(snapshot.AffinityMask);
        Assert.NotNull(snapshot.AllowedCpus);
    }

    [Fact]
    public void Capture_AllowedCpus_MatchesAffinityMask()
    {
        if (!ProcessorAffinityHelper.IsSupported)
        {
            return;
        }

        using var process = Process.GetCurrentProcess();

        CpuConfigurationSnapshot snapshot =
            CpuConfigurationSnapshot.Capture(process);

        Assert.NotNull(snapshot.AffinityMask);
        Assert.Equal(
            ProcessorAffinityHelper.GetAllowedCpus(snapshot.AffinityMask.Value),
            snapshot.AllowedCpus);
    }
}
