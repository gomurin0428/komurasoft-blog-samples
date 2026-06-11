namespace KomuraSoft.AsyncPatterns;

/// <summary>
/// 返り値はまず Task / Task&lt;T&gt; を選ぶ例です（記事 4.1）。
/// await する処理がないなら、無理に async を付けずに
/// Task.CompletedTask や Task.FromResult を返すほうが素直です。
/// メソッド名には Async サフィックスを付けます。
/// </summary>
public sealed class CounterStore
{
    private readonly int _count;

    public CounterStore(int count)
    {
        _count = count;
    }

    public Task SaveAsync(CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }

    public Task<int> CountAsync(CancellationToken cancellationToken)
    {
        return Task.FromResult(_count);
    }
}
