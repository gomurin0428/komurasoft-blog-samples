using Xunit;

namespace KomuraSoft.FileIntegration.Tests;

public class ProcessedLedgerTests : IDisposable
{
    private readonly TempDirectory _temp = new();

    public void Dispose() => _temp.Dispose();

    [Fact]
    public void TryRecordProcessed_FirstCallWins_DuplicateFails()
    {
        var ledger = new ProcessedLedger(_temp.Combine("ledger"));

        Assert.False(ledger.AlreadyProcessed("integration-0001"));
        Assert.True(ledger.TryRecordProcessed("integration-0001"));
        Assert.True(ledger.AlreadyProcessed("integration-0001"));
        Assert.False(ledger.TryRecordProcessed("integration-0001")); // 二重記録は失敗する
    }

    [Fact]
    public async Task TryRecordProcessed_ExactlyOneWinnerUnderConcurrency()
    {
        // 排他が破れて同じ idempotency key が同時に来ても、記録に成功するのは 1 回だけ
        var ledger = new ProcessedLedger(_temp.Combine("ledger"));
        const int workers = 16;

        using var start = new ManualResetEventSlim(false);
        Task<bool>[] attempts = Enumerable.Range(0, workers)
            .Select(_ => Task.Run(() =>
            {
                start.Wait();
                return ledger.TryRecordProcessed("contended-key");
            }))
            .ToArray();

        start.Set();
        bool[] results = await Task.WhenAll(attempts);

        Assert.Equal(1, results.Count(r => r));
    }

    [Fact]
    public void TryRecordProcessed_AcceptsKeysWithUnsafeFileNameCharacters()
    {
        var ledger = new ProcessedLedger(_temp.Combine("ledger"));
        const string key = "orders/2026-03-07T10:00:00+09:00";

        Assert.True(ledger.TryRecordProcessed(key));
        Assert.True(ledger.AlreadyProcessed(key));
    }
}
