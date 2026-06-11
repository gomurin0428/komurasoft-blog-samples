using Xunit;

namespace KomuraSoft.Impersonation.Tests;

// Windows 専用 API（LogonUser / RunImpersonated）の実体は Linux では動かないため、
// ここでは OS に依存しない範囲（引数検証と非 Windows でのガード動作）だけを検証します。
// SafeAccessTokenHandle は非 Windows ではコンストラクターすら PlatformNotSupportedException を
// 投げるため、トークン引数には null を渡し、トークンより先に検証される引数とガードを確認します。
// 実際の偽装動作の確認手順は README.md の「Windows での確認手順」を参照してください。
public class ImpersonatedFileAccessTests
{
    [Theory]
    [InlineData(null)]
    [InlineData("")]
    public void ReadFileAsUser_NullOrEmptyPath_ThrowsArgumentException(string? path)
    {
        // path はトークンより先に検証される。
        // null は ArgumentNullException、空文字は ArgumentException（前者は後者の派生型）
        Assert.ThrowsAny<ArgumentException>(
            () => ImpersonatedFileAccess.ReadFileAsUser(null!, path!));
    }

    [Fact]
    public void ReadFileAsUser_OnWindows_NullToken_ThrowsArgumentNullException()
    {
        if (!OperatingSystem.IsWindows())
        {
            return; // 非 Windows ではトークン検証より先にプラットフォームガードが働く
        }

        Assert.Throws<ArgumentNullException>(
            () => ImpersonatedFileAccess.ReadFileAsUser(null!, @"C:\Data\foo.txt"));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    public async Task WriteFileAsUserAsync_NullOrEmptyPath_ThrowsArgumentException(string? path)
    {
        await Assert.ThrowsAnyAsync<ArgumentException>(
            () => ImpersonatedFileAccess.WriteFileAsUserAsync(
                null!, path!, "text", CancellationToken.None));
    }

    [Fact]
    public async Task WriteFileAsUserAsync_NullText_ThrowsArgumentNullException()
    {
        await Assert.ThrowsAsync<ArgumentNullException>(
            () => ImpersonatedFileAccess.WriteFileAsUserAsync(
                null!, @"C:\Data\foo.txt", null!, CancellationToken.None));
    }

    [Fact]
    public async Task WriteFileAsUserAsync_OnWindows_NullToken_ThrowsArgumentNullException()
    {
        if (!OperatingSystem.IsWindows())
        {
            return; // 非 Windows ではトークン検証より先にプラットフォームガードが働く
        }

        await Assert.ThrowsAsync<ArgumentNullException>(
            () => ImpersonatedFileAccess.WriteFileAsUserAsync(
                null!, @"C:\Data\foo.txt", "text", CancellationToken.None));
    }

    [Theory]
    [InlineData(null, "P@ss", @"C:\Data\foo.txt")] // ユーザー名なし
    [InlineData("", "P@ss", @"C:\Data\foo.txt")]
    [InlineData("alice", null, @"C:\Data\foo.txt")] // パスワードなし
    [InlineData("alice", "", @"C:\Data\foo.txt")]
    [InlineData("alice", "P@ss", null)] // パスなし
    [InlineData("alice", "P@ss", "")]
    public void ReadFileWithExplicitCredential_InvalidArguments_ThrowsArgumentException(
        string? userName,
        string? password,
        string? path)
    {
        Assert.ThrowsAny<ArgumentException>(
            () => ImpersonatedFileAccess.ReadFileWithExplicitCredential(
                userName!, domain: null, password!, path!));
    }

    [Fact]
    public void ReadFileAsUser_OnNonWindows_ThrowsPlatformNotSupportedException()
    {
        if (OperatingSystem.IsWindows())
        {
            return; // Windows では実際の偽装処理へ進むため、このガードは対象外
        }

        Assert.Throws<PlatformNotSupportedException>(
            () => ImpersonatedFileAccess.ReadFileAsUser(null!, @"C:\Data\foo.txt"));
    }

    [Fact]
    public async Task WriteFileAsUserAsync_OnNonWindows_ThrowsPlatformNotSupportedException()
    {
        if (OperatingSystem.IsWindows())
        {
            return;
        }

        await Assert.ThrowsAsync<PlatformNotSupportedException>(
            () => ImpersonatedFileAccess.WriteFileAsUserAsync(
                null!, @"C:\Data\foo.txt", "text", CancellationToken.None));
    }

    [Fact]
    public void ReadFileWithExplicitCredential_OnNonWindows_ThrowsPlatformNotSupportedException()
    {
        if (OperatingSystem.IsWindows())
        {
            return;
        }

        Assert.Throws<PlatformNotSupportedException>(
            () => ImpersonatedFileAccess.ReadFileWithExplicitCredential(
                "alice", "EXAMPLE", "P@ss", @"C:\Data\foo.txt"));
    }
}
