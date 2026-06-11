using System.Management.Automation;
using Xunit;

namespace KomuraSoft.PowerShellObjects.Tests;

public class ProcessSummaryMapperTests
{
    private static PSObject BuildRow(string name, int id, object? cpu, long workingSet)
    {
        // Select-Object や [pscustomobject] が返す行を模した PSObject を作る
        var row = new PSObject();
        row.Properties.Add(new PSNoteProperty("Name", name));
        row.Properties.Add(new PSNoteProperty("Id", id));
        row.Properties.Add(new PSNoteProperty("CPU", cpu));
        row.Properties.Add(new PSNoteProperty("WorkingSet", workingSet));
        return row;
    }

    [Fact]
    public void ToProcessSummary_ReadsAllColumns()
    {
        PSObject row = BuildRow("pwsh", 1234, 1.5, 1024 * 1024);

        ProcessSummary summary = ProcessSummaryMapper.ToProcessSummary(row);

        Assert.Equal("pwsh", summary.Name);
        Assert.Equal(1234, summary.Id);
        Assert.Equal(1.5, summary.Cpu);
        Assert.Equal(1024 * 1024, summary.WorkingSet);
    }

    [Fact]
    public void ToProcessSummary_AllowsNullCpu()
    {
        // CPU は取得できないプロセスでは null になることがある
        PSObject row = BuildRow("idle", 0, cpu: null, workingSet: 0);

        ProcessSummary summary = ProcessSummaryMapper.ToProcessSummary(row);

        Assert.Null(summary.Cpu);
    }

    [Fact]
    public void GetString_ReturnsEmptyString_WhenPropertyIsMissing()
    {
        var row = new PSObject();

        string value = ProcessSummaryMapper.GetString(row, "Name");

        Assert.Equal("", value);
    }

    [Fact]
    public void GetNullableDouble_ConvertsNumericValue()
    {
        PSObject row = BuildRow("pwsh", 1, cpu: 12, workingSet: 0);

        // PowerShell からは int で返ることもあるため、Convert.ToDouble で吸収する
        Assert.Equal(12.0, ProcessSummaryMapper.GetNullableDouble(row, "CPU"));
    }
}
