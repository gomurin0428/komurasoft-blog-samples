using System.Threading.Tasks;
using KomuraSoft.NativeAotSample;
using Xunit;

namespace NativeAotSample.Tests;

/// <summary>
/// export の中身である AccumulatorStore（ふつうの C# ロジック側）を検証します。
/// UnmanagedCallersOnly のメソッドは managed コードから直接呼べないため、
/// 「export メソッドは薄く、本体は別に置く」分業のおかげでここを通常の xUnit でテストできます。
/// </summary>
public class AccumulatorStoreTests
{
    [Fact]
    public void Create_ReturnsOkAndNonZeroHandle()
    {
        var status = AccumulatorStore.Create(out var handle);

        Assert.Equal(NativeStatus.Ok, status);
        Assert.NotEqual(0, handle);
    }

    [Fact]
    public void Create_ReturnsDistinctHandles()
    {
        Assert.Equal(NativeStatus.Ok, AccumulatorStore.Create(out var first));
        Assert.Equal(NativeStatus.Ok, AccumulatorStore.Create(out var second));

        Assert.NotEqual(first, second);

        AccumulatorStore.Destroy(first);
        AccumulatorStore.Destroy(second);
    }

    [Fact]
    public void Add_AccumulatesIntoTotal()
    {
        AccumulatorStore.Create(out var handle);

        Assert.Equal(NativeStatus.Ok, AccumulatorStore.Add(handle, 10));
        Assert.Equal(NativeStatus.Ok, AccumulatorStore.Add(handle, 20));

        Assert.Equal(NativeStatus.Ok, AccumulatorStore.GetTotal(handle, out var total));
        Assert.Equal(30L, total);

        AccumulatorStore.Destroy(handle);
    }

    [Fact]
    public void Add_NegativeValues_AreAccumulated()
    {
        AccumulatorStore.Create(out var handle);

        AccumulatorStore.Add(handle, 100);
        AccumulatorStore.Add(handle, -30);

        AccumulatorStore.GetTotal(handle, out var total);
        Assert.Equal(70L, total);

        AccumulatorStore.Destroy(handle);
    }

    [Fact]
    public void Total_DoesNotOverflowAtInt32Boundary()
    {
        AccumulatorStore.Create(out var handle);

        AccumulatorStore.Add(handle, int.MaxValue);
        AccumulatorStore.Add(handle, int.MaxValue);

        AccumulatorStore.GetTotal(handle, out var total);
        Assert.Equal(2L * int.MaxValue, total);

        AccumulatorStore.Destroy(handle);
    }

    [Fact]
    public void Instances_AreIndependentPerHandle()
    {
        AccumulatorStore.Create(out var first);
        AccumulatorStore.Create(out var second);

        AccumulatorStore.Add(first, 1);
        AccumulatorStore.Add(second, 100);

        AccumulatorStore.GetTotal(first, out var firstTotal);
        AccumulatorStore.GetTotal(second, out var secondTotal);

        Assert.Equal(1L, firstTotal);
        Assert.Equal(100L, secondTotal);

        AccumulatorStore.Destroy(first);
        AccumulatorStore.Destroy(second);
    }

    [Fact]
    public void Add_UnknownHandle_ReturnsInvalidHandle()
    {
        Assert.Equal(NativeStatus.InvalidHandle, AccumulatorStore.Add(unchecked((nint)(-12345)), 1));
    }

    [Fact]
    public void GetTotal_UnknownHandle_ReturnsInvalidHandleAndZeroTotal()
    {
        var status = AccumulatorStore.GetTotal(unchecked((nint)(-12345)), out var total);

        Assert.Equal(NativeStatus.InvalidHandle, status);
        Assert.Equal(0L, total);
    }

    [Fact]
    public void Destroy_UnknownHandle_ReturnsInvalidHandle()
    {
        Assert.Equal(NativeStatus.InvalidHandle, AccumulatorStore.Destroy(unchecked((nint)(-12345))));
    }

    [Fact]
    public void Destroy_Twice_SecondCallReturnsInvalidHandle()
    {
        AccumulatorStore.Create(out var handle);

        Assert.Equal(NativeStatus.Ok, AccumulatorStore.Destroy(handle));
        Assert.Equal(NativeStatus.InvalidHandle, AccumulatorStore.Destroy(handle));
    }

    [Fact]
    public void Add_AfterDestroy_ReturnsInvalidHandle()
    {
        AccumulatorStore.Create(out var handle);
        AccumulatorStore.Destroy(handle);

        Assert.Equal(NativeStatus.InvalidHandle, AccumulatorStore.Add(handle, 1));
    }

    [Fact]
    public async Task Add_FromMultipleThreads_TotalIsConsistent()
    {
        AccumulatorStore.Create(out var handle);

        const int threads = 8;
        const int perThread = 1000;

        var tasks = new Task[threads];
        for (var i = 0; i < threads; i++)
        {
            tasks[i] = Task.Run(() =>
            {
                for (var j = 0; j < perThread; j++)
                {
                    Assert.Equal(NativeStatus.Ok, AccumulatorStore.Add(handle, 1));
                }
            });
        }

        await Task.WhenAll(tasks);

        AccumulatorStore.GetTotal(handle, out var total);
        Assert.Equal((long)threads * perThread, total);

        AccumulatorStore.Destroy(handle);
    }
}
