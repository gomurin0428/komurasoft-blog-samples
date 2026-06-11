using Xunit;

namespace KomuraSoft.AsyncPatterns.Tests;

public class CounterStoreTests
{
    [Fact]
    public async Task SaveAsync_CompletesSynchronously_WithoutAsyncStateMachine()
    {
        var store = new CounterStore(count: 42);

        Task save = store.SaveAsync(CancellationToken.None);

        // await する処理がないので Task.CompletedTask がそのまま返る
        Assert.True(save.IsCompletedSuccessfully);
        await save;
    }

    [Fact]
    public async Task CountAsync_ReturnsCount()
    {
        var store = new CounterStore(count: 42);

        int count = await store.CountAsync(CancellationToken.None);

        Assert.Equal(42, count);
    }
}
