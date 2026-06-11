using System.Text;
using KomuraSoft.AsyncPatterns;
using KomuraSoft.AsyncPatterns.Demo;

// 記事「C# async/await実務判断表 - Task.RunとConfigureAwait」の判断表に登場する
// 各パターンを、外部ネットワークに出ない形で順番に実演するデモです。
// HTTP はスタブ（StubHttpMessageHandler）で代用しています。

using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(60));
CancellationToken cancellationToken = cts.Token;

string workDir = Directory.CreateTempSubdirectory("async-patterns-demo").FullName;
Directory.SetCurrentDirectory(workDir);

using var httpClient = new HttpClient(new StubHttpMessageHandler());
var downloader = new HttpDownloader(httpClient);

// --- 3.2 I/O 待ちなら、async API をそのまま await する ---
Console.WriteLine("== 3.2 I/O 待ちは async API をそのまま await ==");
string textPath = Path.Combine(workDir, "input.txt");
await File.WriteAllTextAsync(textPath, "こんにちは、async/await。", cancellationToken);
string text = await FileTextLoader.LoadTextAsync(textPath, cancellationToken);
Console.WriteLine($"loaded: {text}");

// --- 3.3 CPU 負荷が重いなら、Task.Run を使う場所を選ぶ ---
Console.WriteLine();
Console.WriteLine("== 3.3 重い CPU 計算は Task.Run で今のスレッドから外す ==");
byte[] hash = await CpuBoundHasher.HashManyTimesAsync(
    Encoding.UTF8.GetBytes("komura"),
    repeat: 100_000,
    cancellationToken);
Console.WriteLine($"SHA-256 x 100,000 = {Convert.ToHexString(hash)[..16]}...");

// --- 3.4 独立した複数処理なら、Task.WhenAll ---
Console.WriteLine();
Console.WriteLine("== 3.4 独立した複数処理は Task.WhenAll ==");
string[] pages = await downloader.DownloadAllAsync(
    ["https://example.test/a", "https://example.test/b", "https://example.test/c"],
    cancellationToken);
foreach (string page in pages)
{
    Console.WriteLine($"downloaded: {page}");
}

// --- 3.5 最初に終わったものを使うなら、Task.WhenAny ---
Console.WriteLine();
Console.WriteLine("== 3.5 最初に終わったものだけ使うなら Task.WhenAny ==");
byte[] fastest = await downloader.DownloadFromFirstMirrorAsync(
    [
        "https://mirror1.test/file?delay=300",
        "https://mirror2.test/file?delay=10",
        "https://mirror3.test/file?delay=300",
    ],
    cancellationToken);
Console.WriteLine($"first mirror responded: {Encoding.UTF8.GetString(fastest)}");

// --- 3.6 件数が多いなら、並列数を制限する ---
Console.WriteLine();
Console.WriteLine("== 3.6 件数が多いなら Parallel.ForEachAsync で並列数を制限 ==");
Directory.CreateDirectory("cache");
string[] manyUrls = Enumerable.Range(0, 20)
    .Select(i => $"https://example.test/page-{i}?delay=20")
    .ToArray();
await downloader.DownloadAndSaveAsync(manyUrls, cancellationToken);
Console.WriteLine($"saved {Directory.GetFiles("cache", "*.html").Length} files into ./cache (MaxDegreeOfParallelism = 8)");

// --- 3.7 順番に流したいなら、Channel<T> ---
Console.WriteLine();
Console.WriteLine("== 3.7 順番に流すなら Channel<T>（BackgroundTaskQueue）==");
var queue = new BackgroundTaskQueue();
for (int i = 1; i <= 3; i++)
{
    int number = i;
    await queue.EnqueueAsync(async token =>
    {
        await Task.Delay(10, token);
        Console.WriteLine($"work item {number} processed");
    }, cancellationToken);
}
for (int i = 0; i < 3; i++)
{
    Func<CancellationToken, ValueTask> workItem = await queue.DequeueAsync(cancellationToken);
    await workItem(cancellationToken); // コンシューマが順番に await して処理する
}

// --- 3.8 一定間隔で回したいなら、PeriodicTimer ---
Console.WriteLine();
Console.WriteLine("== 3.8 一定間隔なら PeriodicTimer ==");
int tickCount = 0;
using var periodicCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
var refresher = new PeriodicCacheRefresher(TimeSpan.FromMilliseconds(100), token =>
{
    tickCount++;
    Console.WriteLine($"cache refreshed ({tickCount})");
    if (tickCount >= 3)
    {
        periodicCts.Cancel();
    }
    return Task.CompletedTask;
});
try
{
    await refresher.RunPeriodicAsync(periodicCts.Token);
}
catch (OperationCanceledException)
{
    Console.WriteLine("periodic loop stopped by CancellationToken");
}

// --- 3.9 逐次的に届くデータなら、IAsyncEnumerable<T> ---
Console.WriteLine();
Console.WriteLine("== 3.9 逐次ストリームは IAsyncEnumerable<T> / await foreach ==");
var repository = new InMemoryUserRepository(
    new User(1, "Alice"),
    new User(2, "Bob"),
    new User(3, "Carol"));
var processor = new UserStreamProcessor(repository, (user, _) =>
{
    Console.WriteLine($"processed user {user.Id}: {user.Name}");
    return Task.CompletedTask;
});
await processor.ProcessUsersAsync(cancellationToken);

// --- 4.5 LINQ でタスクを作るときは ToArray で確定する ---
Console.WriteLine();
Console.WriteLine("== 4.5 LINQ + Task.WhenAll は ToArray で確定してから待つ ==");
User[] users = await processor.GetUsersAsync([1, 2, 3], cancellationToken);
Console.WriteLine($"fetched {users.Length} users in parallel: {string.Join(", ", users.Select(u => u.Name))}");

// --- 3.10 非同期で破棄したいなら、await using ---
Console.WriteLine();
Console.WriteLine("== 3.10 非同期破棄は await using ==");
string binPath = Path.Combine(workDir, "output.bin");
await AsyncFileWriter.WriteFileAsync(binPath, hash, cancellationToken);
Console.WriteLine($"wrote {new FileInfo(binPath).Length} bytes with await using");

// --- 3.11 await をまたぐ排他なら、SemaphoreSlim ---
Console.WriteLine();
Console.WriteLine("== 3.11 await をまたぐ排他は SemaphoreSlim ==");
int concurrent = 0;
var cacheRefresher = new CacheRefresher(async token =>
{
    int now = Interlocked.Increment(ref concurrent);
    Console.WriteLine($"refresh running (concurrency = {now})");
    await Task.Delay(50, token);
    Interlocked.Decrement(ref concurrent);
});
await Task.WhenAll(
    cacheRefresher.RefreshAsync(cancellationToken),
    cacheRefresher.RefreshAsync(cancellationToken),
    cacheRefresher.RefreshAsync(cancellationToken));
Console.WriteLine("3 refreshes completed, always one at a time");

Console.WriteLine();
Console.WriteLine("[demo] done");

/// <summary>
/// IAsyncEnumerable のデモ用に、登録済みユーザーを 1 件ずつ非同期に流すリポジトリです。
/// </summary>
internal sealed class InMemoryUserRepository : IUserRepository
{
    private readonly User[] _users;

    public InMemoryUserRepository(params User[] users)
    {
        _users = users;
    }

    public async IAsyncEnumerable<User> StreamUsersAsync(
        [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken cancellationToken)
    {
        foreach (User user in _users)
        {
            await Task.Delay(10, cancellationToken); // ページング API などの待ちの代わり
            yield return user;
        }
    }

    public async Task<User> GetAsync(int id, CancellationToken cancellationToken)
    {
        await Task.Delay(10, cancellationToken);
        return _users.Single(user => user.Id == id);
    }
}
