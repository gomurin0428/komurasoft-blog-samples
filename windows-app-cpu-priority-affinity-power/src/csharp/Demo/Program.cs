using System.ComponentModel;
using System.Diagnostics;
using KomuraSoft.CpuSettings;

// 自プロセスの CPU 関連設定を観察し、優先度とアフィニティを一時的に変更して戻すデモです。
// 記事 2 章（優先度）、3 章（アフィニティ）、14 章（ログ設計）に対応します。

using var process = Process.GetCurrentProcess();

Console.WriteLine("=== 1. 現在の状態を記録する（記事 14 章: 実装より先に、ログを設計する） ===");
PrintSnapshot(CpuConfigurationSnapshot.Capture(process));

Console.WriteLine();
Console.WriteLine("=== 2. アフィニティマスクの組み立てと分解（記事 3 章） ===");
nint mask = ProcessorAffinityHelper.CreateMask(0, 1, 2, 3);
Console.WriteLine($"CreateMask(0, 1, 2, 3) = 0x{mask:X}（2進数で 1111）");
Console.WriteLine($"GetAllowedCpus(0x{mask:X}) = [{string.Join(", ", ProcessorAffinityHelper.GetAllowedCpus(mask))}]");

Console.WriteLine();
Console.WriteLine("=== 3. アフィニティを一時的に絞って戻す（検証用。記事 3 章） ===");

if (ProcessorAffinityHelper.IsSupported && Environment.ProcessorCount > 1)
{
    nint original = ProcessorAffinityHelper.GetAffinityMask(process);
    nint narrowed = ProcessorAffinityHelper.CreateMask(0);

    ProcessorAffinityHelper.SetAffinityMask(process, narrowed);
    Console.WriteLine($"絞った後  : 0x{ProcessorAffinityHelper.GetAffinityMask(process):X}（論理プロセッサ 0 のみ）");

    ProcessorAffinityHelper.SetAffinityMask(process, original);
    Console.WriteLine($"戻した後  : 0x{ProcessorAffinityHelper.GetAffinityMask(process):X}");
}
else
{
    Console.WriteLine("この環境ではアフィニティの変更をスキップします。");
}

Console.WriteLine();
Console.WriteLine("=== 4. 優先度を一時的に変更して戻す（記事 12 章: 優先度は一時的に使う） ===");

if (ProcessPriorityHelper.IsSupported)
{
    try
    {
        // 急がない処理を想定して、一時的に優先度を下げる例。
        // 記事のとおり、上げる場合（AboveNormal など）も「終わったら戻す」が基本です。
        // Linux で優先度を上げ直すには権限（root など）が必要です。
        using (var scope = new TemporaryPriorityScope(process, ProcessPriorityClass.BelowNormal))
        {
            Console.WriteLine($"変更前: {scope.OriginalPriorityClass}");
            Console.WriteLine($"変更中: {ProcessPriorityHelper.GetPriorityClass(process)}");
        }

        process.Refresh();
        Console.WriteLine($"復元後: {ProcessPriorityHelper.GetPriorityClass(process)}");
    }
    catch (Win32Exception ex)
    {
        Console.WriteLine($"権限不足のため優先度を変更できませんでした: {ex.Message}");
    }
}
else
{
    Console.WriteLine("この環境では優先度の変更をスキップします。");
}

Console.WriteLine();
Console.WriteLine("EcoQoS（記事 8 章）の C++ コードは docs/EcoQoS.cpp を参照してください（Windows 専用・参照用）。");

static void PrintSnapshot(CpuConfigurationSnapshot snapshot)
{
    Console.WriteLine($"ProcessId      : {snapshot.ProcessId}");
    Console.WriteLine($"ProcessName    : {snapshot.ProcessName}");
    Console.WriteLine($"OSDescription  : {snapshot.OSDescription}");
    Console.WriteLine($"ProcessorCount : {snapshot.ProcessorCount}");
    Console.WriteLine($"PriorityClass  : {(snapshot.PriorityClass?.ToString() ?? "(未サポート)")}");
    Console.WriteLine($"AffinityMask   : {(snapshot.AffinityMask is { } m ? $"0x{m:X}" : "(未サポート)")}");
    Console.WriteLine($"AllowedCpus    : {(snapshot.AllowedCpus is { } cpus ? $"[{string.Join(", ", cpus)}]" : "(未サポート)")}");
}
