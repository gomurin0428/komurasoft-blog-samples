using Xunit;

namespace KomuraSoft.FileWatching.Tests;

public class FileProcessedStoreTests
{
    [Fact]
    public void AlreadyProcessed_ReturnsFalse_ForUnknownKey()
    {
        using var dir = new TestDirectory();
        var store = new FileProcessedStore(dir.ProcessedDir);

        Assert.False(store.AlreadyProcessed("order-001"));
    }

    [Fact]
    public void RecordProcessed_MakesKeyVisible_AlsoFromAnotherStoreInstance()
    {
        using var dir = new TestDirectory();
        var store = new FileProcessedStore(dir.ProcessedDir);

        store.RecordProcessed("order-001");

        Assert.True(store.AlreadyProcessed("order-001"));
        Assert.False(store.AlreadyProcessed("order-002"));

        // 別ワーカー（別インスタンス）からも同じ記録が見える
        var otherWorkerStore = new FileProcessedStore(dir.ProcessedDir);
        Assert.True(otherWorkerStore.AlreadyProcessed("order-001"));
    }

    [Fact]
    public void RecordProcessed_IsIdempotent()
    {
        using var dir = new TestDirectory();
        var store = new FileProcessedStore(dir.ProcessedDir);

        store.RecordProcessed("order-001");
        store.RecordProcessed("order-001"); // 2 回記録しても例外にならない

        Assert.True(store.AlreadyProcessed("order-001"));
    }

    [Fact]
    public void RecordProcessed_AcceptsKeysThatAreNotValidFileNames()
    {
        using var dir = new TestDirectory();
        var store = new FileProcessedStore(dir.ProcessedDir);

        const string key = "orders/2026-03-10:00#01?*";
        store.RecordProcessed(key);

        Assert.True(store.AlreadyProcessed(key));
    }
}
