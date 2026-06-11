using System.Runtime.CompilerServices;
using Xunit;

namespace KomuraSoft.MemoryDiagnostics.Tests;

/// <summary>
/// 「参照されている限り GC は回収しない」「参照を外せば回収される」を
/// WeakReference とフル GC（記事 7 章の調査用 GC）で観測するテストです。
/// 記事 2 章の「リーク = 意図しない保持」をコードで確認します。
/// </summary>
public class LeakObservationTests
{
    // --- 11.1 static コレクション ---

    [Fact]
    public void StaticCollection_KeepsCustomerAlive_AfterFullGc()
    {
        WeakReference reference = AddCustomerToStaticStore();

        DiagnosticGc.ForceFullGcForDiagnosticsOnly();

        // static な List<Customer> から参照されているため、GC からは「使用中」に見える
        Assert.True(reference.IsAlive);

        Leaky.CustomerStore.ClearForDemo();
        DiagnosticGc.ForceFullGcForDiagnosticsOnly();

        // 参照を外せば回収される（リークの正体は「参照の残り」だと分かる）
        Assert.False(reference.IsAlive);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static WeakReference AddCustomerToStaticStore()
    {
        var customer = new Customer("customer-static");
        Leaky.CustomerStore.Add(customer);
        return new WeakReference(customer);
    }

    // --- 11.3 イベント購読解除漏れ ---

    [Fact]
    public void LeakyViewModel_IsKeptAlive_ByPublisherEvent()
    {
        var singletonService = new OrderService();

        WeakReference reference = CreateLeakyViewModel(singletonService);

        DiagnosticGc.ForceFullGcForDiagnosticsOnly();

        // 長命な OrderService のイベント delegate が ViewModel を参照し続ける
        Assert.True(reference.IsAlive);
        Assert.Equal(1, singletonService.HandlerCount);
    }

    [Fact]
    public void FixedViewModel_IsCollected_AfterDispose()
    {
        var singletonService = new OrderService();

        WeakReference reference = CreateAndDisposeFixedViewModel(singletonService);

        DiagnosticGc.ForceFullGcForDiagnosticsOnly();

        // Dispose で購読解除すれば、publisher からの参照が外れて回収される
        Assert.False(reference.IsAlive);
        Assert.Equal(0, singletonService.HandlerCount);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static WeakReference CreateLeakyViewModel(OrderService service)
        => new(new Leaky.OrderViewModel(service));

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static WeakReference CreateAndDisposeFixedViewModel(OrderService service)
    {
        var viewModel = new Fixed.OrderViewModel(service);
        viewModel.Dispose();
        return new WeakReference(viewModel);
    }

    // --- 11.4 Timer の破棄漏れ ---

    [Fact]
    public void LeakyPollingWorker_IsKeptAlive_ByActiveTimer()
    {
        WeakReference reference = CreateLeakyPollingWorker();

        DiagnosticGc.ForceFullGcForDiagnosticsOnly();

        // アクティブな Timer はタイマーキューに登録され、
        // コールバック delegate 経由で Worker への参照がつながる
        Assert.True(reference.IsAlive);
    }

    [Fact]
    public void FixedPollingWorker_IsCollected_AfterDispose()
    {
        WeakReference reference = CreateAndDisposeFixedPollingWorker();

        DiagnosticGc.ForceFullGcForDiagnosticsOnly();

        Assert.False(reference.IsAlive);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static WeakReference CreateLeakyPollingWorker()
        => new(new Leaky.PollingWorker());

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static WeakReference CreateAndDisposeFixedPollingWorker()
    {
        var worker = new Fixed.PollingWorker();
        worker.Dispose();
        return new WeakReference(worker);
    }
}
