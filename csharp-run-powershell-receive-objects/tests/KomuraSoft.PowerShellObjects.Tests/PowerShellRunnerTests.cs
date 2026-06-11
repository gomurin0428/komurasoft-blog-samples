using System.Diagnostics;
using System.Management.Automation;
using Xunit;

namespace KomuraSoft.PowerShellObjects.Tests;

public class PowerShellRunnerTests
{
    [Fact]
    public void Run_GetProcess_ReturnsProcessAsBaseObject()
    {
        // Get-Process をそのまま実行すると、BaseObject に元の .NET オブジェクトが入る（記事 3 章・4 章）
        PowerShellRunResult result = PowerShellRunner.Run(ps => ps
            .AddCommand("Get-Process")
            .AddParameter("Id", Environment.ProcessId));

        PSObject item = Assert.Single(result.Output);
        Process process = Assert.IsType<Process>(item.BaseObject);
        Assert.Equal(Environment.ProcessId, process.Id);
        Assert.Empty(result.Errors);
    }

    [Fact]
    public void Run_SelectObject_ExposesColumnsViaProperties()
    {
        // Select-Object で列を絞ると元の Process ではなくなるため、Properties から列名で読む（記事 4 章・5 章）
        PowerShellRunResult result = PowerShellRunner.Run(ps => ps
            .AddCommand("Get-Process")
            .AddParameter("Id", Environment.ProcessId)
            .AddCommand("Select-Object")
                .AddParameter("Property", new[] { "Name", "Id", "CPU", "WorkingSet" }));

        PSObject row = Assert.Single(result.Output);
        Assert.IsNotType<Process>(row.BaseObject);
        Assert.Equal(Environment.ProcessId, Convert.ToInt32(row.Properties["Id"]?.Value));
        Assert.False(string.IsNullOrEmpty(row.Properties["Name"]?.Value as string));
    }

    [Fact]
    public void Run_SelectObject_RowsConvertToProcessSummary()
    {
        // PSObject は境界で扱い、アプリ内部では C# の record に変換する（記事 6 章）
        PowerShellRunResult result = PowerShellRunner.Run(ps => ps
            .AddCommand("Get-Process")
            .AddParameter("Id", Environment.ProcessId)
            .AddCommand("Select-Object")
                .AddParameter("Property", new[] { "Name", "Id", "CPU", "WorkingSet" }));

        List<ProcessSummary> processes = result.Output
            .Select(ProcessSummaryMapper.ToProcessSummary)
            .ToList();

        ProcessSummary process = Assert.Single(processes);
        Assert.Equal(Environment.ProcessId, process.Id);
        Assert.False(string.IsNullOrEmpty(process.Name));
        Assert.True(process.WorkingSet >= 0);
    }

    [Fact]
    public void Run_PsCustomObjectScript_IsReadableByPropertyName()
    {
        // PowerShell 側で [pscustomobject] を返すと、C# 側では Properties から名前で読める（記事 7 章）
        const string script = @"
[pscustomobject]@{
    MachineName       = [System.Environment]::MachineName
    PowerShellVersion = $PSVersionTable.PSVersion.ToString()
    CurrentDirectory  = (Get-Location).Path
}
";

        PowerShellRunResult result = PowerShellRunner.Run(ps => ps
            .AddScript(script, useLocalScope: true));

        PSObject row = Assert.Single(result.Output);
        Assert.Equal(Environment.MachineName, row.Properties["MachineName"]?.Value as string);
        Assert.False(string.IsNullOrEmpty(row.Properties["PowerShellVersion"]?.Value as string));
        Assert.False(string.IsNullOrEmpty(row.Properties["CurrentDirectory"]?.Value as string));
    }

    [Fact]
    public void Run_AddParameter_PassesValueSafely()
    {
        // ユーザー入力相当の値はスクリプトに連結せず AddParameter で渡す（記事 8 章）
        string userInputPath = Path.GetTempPath();

        PowerShellRunResult result = PowerShellRunner.Run(ps => ps
            .AddCommand("Get-ChildItem")
            .AddParameter("Path", userInputPath)
            .AddParameter("File", true));

        // 例外にならず、エラーストリームも空であること（出力件数は環境依存なので問わない）
        Assert.Empty(result.Errors);
    }

    [Fact]
    public void Run_CollectsNonTerminatingErrors()
    {
        // 出力とエラーは別ストリーム。戻り値だけでなく Errors も確認する（記事 10 章）
        string missingPath = Path.Combine(
            Path.GetTempPath(),
            $"no-such-file-{Guid.NewGuid():N}.txt");

        PowerShellRunResult result = PowerShellRunner.Run(ps => ps
            .AddCommand("Get-Item")
            .AddParameter("Path", missingPath));

        Assert.Empty(result.Output);
        ErrorRecord error = Assert.Single(result.Errors);
        Assert.Equal(ErrorCategory.ObjectNotFound, error.CategoryInfo.Category);
    }

    [Fact]
    public void Run_ThrowsInvalidOperationException_WhenErrorActionIsStop()
    {
        // ErrorAction Stop を指定すると終了するエラーになり、ラッパーが例外に変換する（記事 10 章・11 章）
        string missingPath = Path.Combine(
            Path.GetTempPath(),
            $"no-such-file-{Guid.NewGuid():N}.txt");

        InvalidOperationException ex = Assert.Throws<InvalidOperationException>(() =>
            PowerShellRunner.Run(ps => ps
                .AddCommand("Get-Item")
                .AddParameter("Path", missingPath)
                .AddParameter("ErrorAction", "Stop")));

        Assert.IsAssignableFrom<RuntimeException>(ex.InnerException);
    }
}
