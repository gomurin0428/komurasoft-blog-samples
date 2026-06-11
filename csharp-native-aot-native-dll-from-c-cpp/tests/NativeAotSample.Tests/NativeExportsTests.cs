using KomuraSoft.NativeAotSample;
using Xunit;

namespace NativeAotSample.Tests;

/// <summary>
/// UnmanagedCallersOnly のメソッドは C# から直接呼び出せませんが、
/// unmanaged 関数ポインタ（delegate*）経由なら in-process で呼べるため、
/// export 層（引数の null チェックや status code の変換）もここで検証します。
/// </summary>
public unsafe class NativeExportsTests
{
    private static readonly delegate* unmanaged[Cdecl]<nint*, int> s_create = &NativeExports.AccumulatorCreate;
    private static readonly delegate* unmanaged[Cdecl]<nint, int, int> s_add = &NativeExports.AccumulatorAdd;
    private static readonly delegate* unmanaged[Cdecl]<nint, long*, int> s_getTotal = &NativeExports.AccumulatorGetTotal;
    private static readonly delegate* unmanaged[Cdecl]<nint, int> s_destroy = &NativeExports.AccumulatorDestroy;

    [Fact]
    public void CreateAddGetTotalDestroy_RoundTripsLikeTheCppSample()
    {
        nint handle = 0;
        Assert.Equal(NativeStatus.Ok, s_create(&handle));
        Assert.NotEqual(0, handle);

        Assert.Equal(NativeStatus.Ok, s_add(handle, 10));
        Assert.Equal(NativeStatus.Ok, s_add(handle, 20));

        long total = 0;
        Assert.Equal(NativeStatus.Ok, s_getTotal(handle, &total));
        Assert.Equal(30L, total);

        Assert.Equal(NativeStatus.Ok, s_destroy(handle));
    }

    [Fact]
    public void Create_NullOutHandle_ReturnsInvalidArgument()
    {
        Assert.Equal(NativeStatus.InvalidArgument, s_create(null));
    }

    [Fact]
    public void GetTotal_NullOutTotal_ReturnsInvalidArgument()
    {
        nint handle = 0;
        s_create(&handle);

        Assert.Equal(NativeStatus.InvalidArgument, s_getTotal(handle, null));

        s_destroy(handle);
    }

    [Fact]
    public void Add_UnknownHandle_ReturnsInvalidHandle()
    {
        Assert.Equal(NativeStatus.InvalidHandle, s_add(unchecked((nint)(-99999)), 1));
    }

    [Fact]
    public void Destroy_UnknownHandle_ReturnsInvalidHandle()
    {
        Assert.Equal(NativeStatus.InvalidHandle, s_destroy(unchecked((nint)(-99999))));
    }
}
