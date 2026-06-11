using Xunit;

namespace KomuraSoft.AdminBroker.Tests;

public class ElevationBrokerClientTests
{
    [Fact]
    public void Ctor_HelperNotFound_ThrowsFileNotFoundException()
    {
        // helper EXE は絶対パスで固定解決し、存在しなければ起動前に失敗させる（記事 5.2 章・10 章)
        string missingPath = Path.Combine(Path.GetTempPath(), $"missing-{Guid.NewGuid():N}", "MyApp.AdminBroker.exe");

        Assert.Throws<FileNotFoundException>(() => new ElevationBrokerClient(missingPath));
    }

    [Fact]
    public async Task SetExplorerContextMenuEnabledAsync_OnNonWindows_ThrowsPlatformNotSupportedException()
    {
        if (OperatingSystem.IsWindows())
        {
            return; // Windows では runas による昇格起動へ進むため、このガードは対象外
        }

        string fakeHelperPath = Path.Combine(Path.GetTempPath(), $"fake-helper-{Guid.NewGuid():N}.exe");
        await File.WriteAllBytesAsync(fakeHelperPath, []);

        try
        {
            var client = new ElevationBrokerClient(fakeHelperPath);

            // UAC 昇格（runas）と Windows ユーザー SID は Windows 専用のため、早期に失敗すること
            await Assert.ThrowsAsync<PlatformNotSupportedException>(
                () => client.SetExplorerContextMenuEnabledAsync(enabled: true));
        }
        finally
        {
            File.Delete(fakeHelperPath);
        }
    }
}
