using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Text.Json;
using KomuraSoft.MemoryDiagnostics;
using Fixed = KomuraSoft.MemoryDiagnostics.Fixed;
using Leaky = KomuraSoft.MemoryDiagnostics.Leaky;

// 「GC 待ち」と「メモリリーク（意図しない保持）」の違いを、同じプロセス内で観測するデモです。
// 既定では各パターンを 1 回ずつ実演して終了します。
//
// `dotnet run --project samples/Demo -- --leak` で起動すると、static コレクションへ
// 追加し続けるリークモードになります。別ターミナルから dotnet-counters / dotnet-gcdump /
// dotnet-dump をアタッチして、記事の観測手順を試せます（手順は README.md 参照）。

if (args.Contains("--leak"))
{
    await RunLeakModeAsync();
    return;
}

Console.WriteLine($"PID: {Environment.ProcessId}");
Console.WriteLine();

DemoGcWaitVsLeak();
DemoEventHandlerLeak();
DemoTimerLeak();
DemoUnboundedCache();
DemoAuditBufferAndPooledBuffer();

Console.WriteLine("=== GC スナップショット（記事 17 章 GcDiagnostics.Snapshot）===");
Console.WriteLine(JsonSerializer.Serialize(
    GcDiagnostics.Snapshot(),
    new JsonSerializerOptions { WriteIndented = true }));
Console.WriteLine();
Console.WriteLine("[demo] done");

// --- 1. GC 待ちとリークの違い -------------------------------------------------

static void DemoGcWaitVsLeak()
{
    Console.WriteLine("=== 1. 「GC 待ち」と「生き残り」の違い（記事 6〜7 章）===");

    DiagnosticGc.ForceFullGcForDiagnosticsOnly();
    long baseline = GC.GetTotalMemory(forceFullCollection: false);

    // 一時オブジェクトを大量に割り当てる（誰も参照を保持しない）
    AllocateTemporaryObjects(count: 10_000);
    long afterAllocation = GC.GetTotalMemory(forceFullCollection: false);

    DiagnosticGc.ForceFullGcForDiagnosticsOnly();
    long afterGc = GC.GetTotalMemory(forceFullCollection: false);

    Console.WriteLine($"  一時割り当て: baseline={baseline / 1024:N0} KB " +
                      $"-> 割り当て後={afterAllocation / 1024:N0} KB " +
                      $"-> フル GC 後={afterGc / 1024:N0} KB（ベースライン付近へ戻る）");

    // 同じ量を static コレクションに追加する（記事 11.1 章）
    for (int i = 0; i < 10_000; i++)
    {
        Leaky.CustomerStore.Add(new Customer($"customer-{i}"));
    }

    DiagnosticGc.ForceFullGcForDiagnosticsOnly();
    long afterLeakGc = GC.GetTotalMemory(forceFullCollection: false);

    Console.WriteLine($"  static 保持:  CustomerStore.Count={Leaky.CustomerStore.Count:N0} 件 " +
                      $"-> フル GC 後={afterLeakGc / 1024:N0} KB（戻らない = 生き残っている）");

    Leaky.CustomerStore.ClearForDemo();
    DiagnosticGc.ForceFullGcForDiagnosticsOnly();
    Console.WriteLine($"  参照を外すと: フル GC 後={GC.GetTotalMemory(false) / 1024:N0} KB（回収される）");
    Console.WriteLine();
}

[MethodImpl(MethodImplOptions.NoInlining)]
static void AllocateTemporaryObjects(int count)
{
    for (int i = 0; i < count; i++)
    {
        _ = new Customer($"temp-{i}");
    }
}

// --- 2. イベント購読解除漏れ ---------------------------------------------------

static void DemoEventHandlerLeak()
{
    Console.WriteLine("=== 2. イベント購読解除漏れ（記事 11.3 章）===");

    var singletonService = new OrderService();

    List<WeakReference> leakyRefs = CreateLeakyViewModels(singletonService, count: 100);
    List<WeakReference> fixedRefs = CreateAndDisposeFixedViewModels(singletonService, count: 100);

    DiagnosticGc.ForceFullGcForDiagnosticsOnly();

    Console.WriteLine($"  購読解除なし: フル GC 後も生存 {leakyRefs.Count(r => r.IsAlive)}/100 件" +
                      $"（OrderService のイベントが参照を持つ）");
    Console.WriteLine($"  Dispose で解除: フル GC 後の生存 {fixedRefs.Count(r => r.IsAlive)}/100 件");
    Console.WriteLine($"  OrderService.HandlerCount = {singletonService.HandlerCount}");
    Console.WriteLine();
}

[MethodImpl(MethodImplOptions.NoInlining)]
static List<WeakReference> CreateLeakyViewModels(OrderService service, int count)
{
    var refs = new List<WeakReference>();
    for (int i = 0; i < count; i++)
    {
        refs.Add(new WeakReference(new Leaky.OrderViewModel(service)));
    }

    return refs;
}

[MethodImpl(MethodImplOptions.NoInlining)]
static List<WeakReference> CreateAndDisposeFixedViewModels(OrderService service, int count)
{
    var refs = new List<WeakReference>();
    for (int i = 0; i < count; i++)
    {
        var viewModel = new Fixed.OrderViewModel(service);
        viewModel.Dispose(); // 画面を閉じるタイミングで購読解除する想定
        refs.Add(new WeakReference(viewModel));
    }

    return refs;
}

// --- 3. Timer の破棄漏れ -------------------------------------------------------

static void DemoTimerLeak()
{
    Console.WriteLine("=== 3. Timer の破棄漏れ（記事 11.4 章）===");

    WeakReference leakyRef = CreateLeakyPollingWorker();
    WeakReference fixedRef = CreateAndDisposeFixedPollingWorker();

    DiagnosticGc.ForceFullGcForDiagnosticsOnly();

    Console.WriteLine($"  Dispose なし: フル GC 後も生存 = {leakyRef.IsAlive}" +
                      $"（タイマーキューがコールバック経由で参照を持つ）");
    Console.WriteLine($"  Dispose あり: フル GC 後も生存 = {fixedRef.IsAlive}");
    Console.WriteLine();
}

[MethodImpl(MethodImplOptions.NoInlining)]
static WeakReference CreateLeakyPollingWorker()
{
    return new WeakReference(new Leaky.PollingWorker());
}

[MethodImpl(MethodImplOptions.NoInlining)]
static WeakReference CreateAndDisposeFixedPollingWorker()
{
    var worker = new Fixed.PollingWorker();
    worker.Dispose();
    return new WeakReference(worker);
}

// --- 4. 無制限キャッシュ -------------------------------------------------------

static void DemoUnboundedCache()
{
    Console.WriteLine("=== 4. 無制限キャッシュ（記事 11.2 章）===");

    var cache = new Leaky.ReportCache();

    // 同じキーなら増えない（これはキャッシュとして正常）
    var fixedDate = new DateTime(2026, 6, 9);
    for (int i = 0; i < 1_000; i++)
    {
        cache.GetOrCreate("user-1", fixedDate);
    }

    Console.WriteLine($"  固定キー 1,000 回: Count = {cache.Count}（増えない）");

    // キーに現在時刻を含めると、呼ぶたびにエントリが増える（実質的なリーク）
    for (int i = 0; i < 1_000; i++)
    {
        cache.GetOrCreate("user-1", DateTime.UtcNow.AddTicks(i));
    }

    Console.WriteLine($"  時刻入りキー 1,000 回: Count = {cache.Count}（呼ぶたびに増える）");
    Console.WriteLine();
}

// --- 5. singleton バッファと ArrayPool ----------------------------------------

static void DemoAuditBufferAndPooledBuffer()
{
    Console.WriteLine("=== 5. singleton バッファと ArrayPool（記事 11.7 / 12 章）===");

    var auditBuffer = new Leaky.AuditBuffer(); // singleton 登録を想定
    for (int i = 0; i < 1_000; i++)
    {
        auditBuffer.Add(new RequestAudit($"/api/report/export?n={i}", DateTimeOffset.UtcNow));
    }

    Console.WriteLine($"  AuditBuffer.Count = {auditBuffer.Count}" +
                      $"（singleton なら上限・送信・削除がない限り増え続ける）");

    PooledBuffers.ProcessWithPooledBuffer(1024 * 1024, buffer =>
    {
        Console.WriteLine($"  ArrayPool から {buffer.Length / 1024:N0} KB を Rent し、finally で Return した");
    });

    Console.WriteLine();
}

// --- リークモード（外部ツールでの観測用） --------------------------------------

static async Task RunLeakModeAsync()
{
    Console.WriteLine($"PID: {Environment.ProcessId}");
    Console.WriteLine("リークモード: static な CustomerStore へ追加し続けます。Ctrl+C で終了します。");
    Console.WriteLine("別ターミナルから観測例:");
    Console.WriteLine($"  dotnet-counters monitor --process-id {Environment.ProcessId} --counters System.Runtime");
    Console.WriteLine($"  dotnet-gcdump collect --process-id {Environment.ProcessId} --output before.gcdump");
    Console.WriteLine();

    var stopwatch = Stopwatch.StartNew();
    int added = 0;

    while (true)
    {
        for (int i = 0; i < 100; i++)
        {
            Leaky.CustomerStore.Add(new Customer($"customer-{added + i}"));
        }

        added += 100;

        if (added % 5_000 == 0)
        {
            Console.WriteLine($"  [{stopwatch.Elapsed:mm\\:ss}] CustomerStore.Count={Leaky.CustomerStore.Count:N0} " +
                              $"GC Heap={GC.GetTotalMemory(false) / 1024 / 1024:N1} MB " +
                              $"Gen2 GC={GC.CollectionCount(2)} 回");
        }

        await Task.Delay(50);
    }
}
