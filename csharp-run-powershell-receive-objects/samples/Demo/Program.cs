using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Globalization;
using System.Management.Automation;
using KomuraSoft.PowerShellObjects;

// C# から PowerShell を実行し、結果を文字列ではなく PSObject として受け取るデモです。
// 記事の各章のコードを順番に実行します。
// 記事中の Get-Service など Windows 固有のコマンドレットを使う例は、
// Linux / macOS でも動くように Get-Process / Get-ChildItem に置き換えています。

Console.WriteLine("=== 1. 最小コード：Get-Process を実行して PSObject を受け取る（記事 3 章） ===");
RunMinimalExample();

Console.WriteLine();
Console.WriteLine("=== 2. Select-Object した結果を Properties で読む（記事 5 章） ===");
RunSelectObjectExample();

Console.WriteLine();
Console.WriteLine("=== 3. PSObject を C# の record に変換する（記事 6 章） ===");
RunRecordConversionExample();

Console.WriteLine();
Console.WriteLine("=== 4. PSCustomObject を返すスクリプトを読む（記事 7 章） ===");
RunPsCustomObjectExample();

Console.WriteLine();
Console.WriteLine("=== 5. AddParameter で値を安全に渡す（記事 8 章） ===");
RunSafeParameterExample();

Console.WriteLine();
Console.WriteLine("=== 6. エラーストリームと ErrorAction Stop（記事 10 章） ===");
RunErrorHandlingExample();

Console.WriteLine();
Console.WriteLine("=== 7. 実行ラッパー PowerShellRunner を使う（記事 11 章） ===");
RunWrapperExample();

Console.WriteLine();
Console.WriteLine("[demo] done");

// 現在の C# アプリ自身のプロセスを PowerShell から取得する（記事 3 章）
static void RunMinimalExample()
{
    int currentProcessId = Environment.ProcessId;

    using PowerShell ps = PowerShell.Create();

    Collection<PSObject> results = ps
        .AddCommand("Get-Process")
        .AddParameter("Id", currentProcessId)
        .Invoke();

    foreach (PSObject item in results)
    {
        Console.WriteLine($"PSObject type: {item.GetType().FullName}");
        Console.WriteLine($"BaseObject type: {item.BaseObject.GetType().FullName}");

        if (item.BaseObject is Process process)
        {
            Console.WriteLine($"Id: {process.Id}");
            Console.WriteLine($"Name: {process.ProcessName}");
            Console.WriteLine($"Memory: {process.WorkingSet64:N0} bytes");
        }
    }
}

// AddCommand を続けて呼んでパイプラインを作り、Select-Object で絞った列を読む（記事 5 章）
static void RunSelectObjectExample()
{
    using PowerShell ps = PowerShell.Create();

    Collection<PSObject> rows = ps
        .AddCommand("Get-Process")
        .AddCommand("Sort-Object")
            .AddParameter("Property", "CPU")
            .AddParameter("Descending", true)
        .AddCommand("Select-Object")
            .AddParameter("First", 10)
            .AddParameter("Property", new[] { "Name", "Id", "CPU", "WorkingSet" })
        .Invoke();

    foreach (PSObject row in rows)
    {
        string name = Convert.ToString(row.Properties["Name"]?.Value, CultureInfo.InvariantCulture) ?? "";
        int id = Convert.ToInt32(row.Properties["Id"]?.Value, CultureInfo.InvariantCulture);

        // CPU は ScriptProperty のため、値が PSObject に包まれたまま返ることがある。
        // Convert.ToDouble に渡す前に中身（BaseObject）を取り出しておく。
        object? cpuValue = row.Properties["CPU"]?.Value;
        if (cpuValue is PSObject wrappedCpu)
        {
            cpuValue = wrappedCpu.BaseObject;
        }

        double? cpu = cpuValue is null
            ? null
            : Convert.ToDouble(cpuValue, CultureInfo.InvariantCulture);
        long workingSet = Convert.ToInt64(row.Properties["WorkingSet"]?.Value, CultureInfo.InvariantCulture);

        Console.WriteLine($"{id}: {name}, CPU={cpu}, WorkingSet={workingSet:N0}");
    }
}

// PSObject は境界で扱い、アプリ内部では ProcessSummary に変換する（記事 6 章）
static void RunRecordConversionExample()
{
    using PowerShell ps = PowerShell.Create();

    Collection<PSObject> rows = ps
        .AddCommand("Get-Process")
        .AddCommand("Sort-Object")
            .AddParameter("Property", "CPU")
            .AddParameter("Descending", true)
        .AddCommand("Select-Object")
            .AddParameter("First", 10)
            .AddParameter("Property", new[] { "Name", "Id", "CPU", "WorkingSet" })
        .Invoke();

    List<ProcessSummary> processes = rows
        .Select(ProcessSummaryMapper.ToProcessSummary)
        .ToList();

    foreach (ProcessSummary process in processes)
    {
        Console.WriteLine($"{process.Id}: {process.Name}");
    }
}

// PowerShell 側で [pscustomobject] を返し、C# 側では Properties から名前で読む（記事 7 章）
static void RunPsCustomObjectExample()
{
    string script = @"
[pscustomobject]@{
    MachineName       = [System.Environment]::MachineName
    PowerShellVersion = $PSVersionTable.PSVersion.ToString()
    CurrentDirectory  = (Get-Location).Path
}
";

    using PowerShell ps = PowerShell.Create();

    Collection<PSObject> rows = ps
        .AddScript(script, useLocalScope: true)
        .Invoke();

    foreach (PSObject row in rows)
    {
        Console.WriteLine($"MachineName: {row.Properties["MachineName"]?.Value}");
        Console.WriteLine($"PowerShell:  {row.Properties["PowerShellVersion"]?.Value}");
        Console.WriteLine($"Directory:   {row.Properties["CurrentDirectory"]?.Value}");
    }
}

// ユーザー入力をスクリプト文字列へ連結せず、AddParameter で値として渡す（記事 8 章）
static void RunSafeParameterExample()
{
    // 実際のアプリではユーザー入力になる想定の値（ここでは一時フォルダーを使用）
    string userInputPath = GetPathFromUser();

    using PowerShell ps = PowerShell.Create();

    Collection<PSObject> files = ps
        .AddCommand("Get-ChildItem")
        .AddParameter("Path", userInputPath)
        .AddParameter("File", true)
        .Invoke();

    Console.WriteLine($"Path: {userInputPath}");
    Console.WriteLine($"File count: {files.Count}");
}

static string GetPathFromUser()
{
    // デモなので固定値を返す。実際のアプリでは画面や引数から受け取る
    return Path.GetTempPath();
}

// 出力とエラーは別のストリーム。エラーストリームの回収と ErrorAction Stop の両方を試す（記事 10 章）
static void RunErrorHandlingExample()
{
    // 記事では C:\no-such-file.txt（Windows のパス）を使っているが、
    // クロスプラットフォームで動くように一時フォルダー配下の存在しないファイルにしている
    string missingPath = Path.Combine(Path.GetTempPath(), "no-such-file.txt");

    using (PowerShell ps = PowerShell.Create())
    {
        Collection<PSObject> output = ps
            .AddCommand("Get-Item")
            .AddParameter("Path", missingPath)
            .Invoke();

        Console.WriteLine($"Output count: {output.Count}");

        if (ps.HadErrors)
        {
            foreach (ErrorRecord error in ps.Streams.Error)
            {
                Console.WriteLine($"Error: {error.Exception.Message}");
                Console.WriteLine($"Category: {error.CategoryInfo.Category}");
                Console.WriteLine($"Target: {error.TargetObject}");
            }
        }
    }

    try
    {
        using PowerShell ps = PowerShell.Create();

        Collection<PSObject> output = ps
            .AddCommand("Get-Item")
            .AddParameter("Path", missingPath)
            .AddParameter("ErrorAction", "Stop")
            .Invoke();

        Console.WriteLine($"Output count: {output.Count}");
    }
    catch (RuntimeException ex)
    {
        Console.WriteLine($"PowerShell failed: {ex.Message}");
    }
}

// 実行ラッパー経由でパイプラインを実行する（記事 11 章）
// 記事では Get-Service | Where-Object Status -EQ Running の例だが、
// Linux / macOS でも動くように Get-Process + WorkingSet の条件に置き換えている
static void RunWrapperExample()
{
    PowerShellRunResult result = PowerShellRunner.Run(ps => ps
        .AddCommand("Get-Process")
        .AddCommand("Where-Object")
            .AddParameter("Property", "WorkingSet")
            .AddParameter("GT", true)
            .AddParameter("Value", 0)
        .AddCommand("Select-Object")
            .AddParameter("First", 10)
            .AddParameter("Property", new[] { "Name", "Id", "WorkingSet" }));

    foreach (PSObject row in result.Output)
    {
        Console.WriteLine($"{row.Properties["Name"]?.Value}: {row.Properties["Id"]?.Value}");
    }

    foreach (ErrorRecord error in result.Errors)
    {
        Console.Error.WriteLine(error.Exception.Message);
    }
}
