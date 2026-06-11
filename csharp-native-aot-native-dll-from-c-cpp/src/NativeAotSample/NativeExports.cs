// NativeExports.cs
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace KomuraSoft.NativeAotSample;

internal static class NativeStatus
{
    public const int Ok = 0;
    public const int InvalidArgument = -1;
    public const int InvalidHandle = -2;
    public const int UnexpectedError = -3;
}

internal sealed class Accumulator
{
    public long Total { get; private set; }

    public void Add(int value)
    {
        Total += value;
    }
}

internal static class AccumulatorStore
{
    private static readonly object s_gate = new();
    private static readonly Dictionary<nint, Accumulator> s_instances = new();
    private static long s_nextHandle = 0;

    public static int Create(out nint handle)
    {
        try
        {
            var instance = new Accumulator();
            handle = (nint)System.Threading.Interlocked.Increment(ref s_nextHandle);

            lock (s_gate)
            {
                s_instances.Add(handle, instance);
            }

            return NativeStatus.Ok;
        }
        catch
        {
            handle = 0;
            return NativeStatus.UnexpectedError;
        }
    }

    public static int Add(nint handle, int value)
    {
        try
        {
            lock (s_gate)
            {
                if (!s_instances.TryGetValue(handle, out var instance))
                {
                    return NativeStatus.InvalidHandle;
                }

                instance.Add(value);
                return NativeStatus.Ok;
            }
        }
        catch
        {
            return NativeStatus.UnexpectedError;
        }
    }

    public static int GetTotal(nint handle, out long total)
    {
        try
        {
            lock (s_gate)
            {
                if (!s_instances.TryGetValue(handle, out var instance))
                {
                    total = 0;
                    return NativeStatus.InvalidHandle;
                }

                total = instance.Total;
                return NativeStatus.Ok;
            }
        }
        catch
        {
            total = 0;
            return NativeStatus.UnexpectedError;
        }
    }

    public static int Destroy(nint handle)
    {
        try
        {
            lock (s_gate)
            {
                return s_instances.Remove(handle)
                    ? NativeStatus.Ok
                    : NativeStatus.InvalidHandle;
            }
        }
        catch
        {
            return NativeStatus.UnexpectedError;
        }
    }
}

public static unsafe class NativeExports
{
    [UnmanagedCallersOnly(
        EntryPoint = "km_accumulator_create",
        CallConvs = new[] { typeof(CallConvCdecl) })]
    public static int AccumulatorCreate(nint* outHandle)
    {
        if (outHandle == null)
        {
            return NativeStatus.InvalidArgument;
        }

        var status = AccumulatorStore.Create(out var handle);
        *outHandle = handle;
        return status;
    }

    [UnmanagedCallersOnly(
        EntryPoint = "km_accumulator_add",
        CallConvs = new[] { typeof(CallConvCdecl) })]
    public static int AccumulatorAdd(nint handle, int value)
    {
        return AccumulatorStore.Add(handle, value);
    }

    [UnmanagedCallersOnly(
        EntryPoint = "km_accumulator_get_total",
        CallConvs = new[] { typeof(CallConvCdecl) })]
    public static int AccumulatorGetTotal(nint handle, long* outTotal)
    {
        if (outTotal == null)
        {
            return NativeStatus.InvalidArgument;
        }

        var status = AccumulatorStore.GetTotal(handle, out var total);
        *outTotal = total;
        return status;
    }

    [UnmanagedCallersOnly(
        EntryPoint = "km_accumulator_destroy",
        CallConvs = new[] { typeof(CallConvCdecl) })]
    public static int AccumulatorDestroy(nint handle)
    {
        return AccumulatorStore.Destroy(handle);
    }
}
