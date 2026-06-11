using Xunit;

namespace DesktopHostSample.Tests;

public class StatusStoreTests
{
    [Fact]
    public void Current_BeforeUpdate_ReturnsEmpty()
    {
        var store = new StatusStore();

        Assert.Equal(DeviceStatus.Empty, store.Current);
        Assert.Equal("No Data", store.Current.Message);
    }

    [Fact]
    public void Current_AfterUpdate_ReturnsLatestStatus()
    {
        var store = new StatusStore();

        store.Update(new DeviceStatus("first"));
        store.Update(new DeviceStatus("second"));

        Assert.Equal(new DeviceStatus("second"), store.Current);
    }

    [Fact]
    public async Task Update_FromMultipleThreads_AlwaysExposesAWrittenValue()
    {
        // worker (BackgroundService) と UI が同時にアクセスする想定の共有層なので、
        // 並行更新中でも「書き込まれたことのある値」だけが読めることを確認する。
        var store = new StatusStore();
        var written = new HashSet<string> { DeviceStatus.Empty.Message };

        for (int i = 0; i < 100; i++)
        {
            written.Add($"status-{i}");
        }

        Task writer = Task.Run(() =>
        {
            for (int i = 0; i < 100; i++)
            {
                store.Update(new DeviceStatus($"status-{i}"));
            }
        });

        Task reader = Task.Run(() =>
        {
            for (int i = 0; i < 100; i++)
            {
                Assert.Contains(store.Current.Message, written);
            }
        });

        await Task.WhenAll(writer, reader);

        Assert.Equal(new DeviceStatus("status-99"), store.Current);
    }
}
