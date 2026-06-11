using Xunit;

namespace KomuraSoft.UiThreadAsyncAwait.Tests;

/// <summary>
/// 記事の中心テーマ「await の続きがどこへ戻るか」を、
/// UI スレッドを模した SingleThreadSynchronizationContext で UI 非依存に検証する。
/// </summary>
public sealed class SynchronizationContextBehaviorTests
{
    /// <summary>
    /// 記事 4.1 章: plain await はその時点の SynchronizationContext を捕まえ、
    /// 続きはキャプチャしたコンテキスト（UI スレッド相当）へ戻る。
    /// </summary>
    [Fact]
    public async Task PlainAwaitの続きはキャプチャしたコンテキストのスレッドへ戻る()
    {
        SingleThreadSynchronizationContext context = new();
        try
        {
            TaskCompletionSource<(int Before, int After)> tcs = new();

            context.Post(_ => RunAsync(), null);

            (int before, int after) = await tcs.Task.WaitAsync(TimeSpan.FromSeconds(10));

            Assert.Equal(context.ThreadId, before);
            Assert.Equal(context.ThreadId, after);

            async void RunAsync()
            {
                int beforeAwait = Environment.CurrentManagedThreadId;
                await Task.Delay(50);
                int afterAwait = Environment.CurrentManagedThreadId;
                tcs.TrySetResult((beforeAwait, afterAwait));
            }
        }
        finally
        {
            context.Complete();
        }
    }

    /// <summary>
    /// 記事 4.3 章: ConfigureAwait(false) は「キャプチャしたコンテキストへ戻ることを強制しない」。
    /// 未完了の await の続きは UI スレッド相当のスレッドには戻らない
    /// （ここで UI を直接触るとクロススレッドアクセスになる）。
    /// </summary>
    [Fact]
    public async Task ConfigureAwaitFalseの続きはコンテキストのスレッドへ戻らない()
    {
        SingleThreadSynchronizationContext context = new();
        try
        {
            TaskCompletionSource<int> tcs = new();

            context.Post(_ => RunAsync(), null);

            int afterAwait = await tcs.Task.WaitAsync(TimeSpan.FromSeconds(10));

            Assert.NotEqual(context.ThreadId, afterAwait);

            async void RunAsync()
            {
                // Task.Delay は未完了の await（即完了なら同じスレッドで続くこともある点に注意）
                await Task.Delay(50).ConfigureAwait(false);
                tcs.TrySetResult(Environment.CurrentManagedThreadId);
            }
        }
        finally
        {
            context.Complete();
        }
    }

    /// <summary>
    /// 記事 4.4 章: UI スレッド相当のスレッドで .Result によって同期的に待つと、
    /// 継続がそのスレッドへ戻れず、互いに待ち合ってデッドロックする。
    ///
    /// やってはいけない例（.Result）の挙動確認であり、
    /// 「2 秒待っても完了しない」ことをもってデッドロックとみなす。
    /// </summary>
    [Fact]
    public async Task コンテキストのスレッドでResultを使うとデッドロックする()
    {
        SingleThreadSynchronizationContext context = new();
        TaskCompletionSource completed = new();

        context.Post(_ =>
        {
            // やってはいけない例: UI スレッド相当のスレッドで .Result によりブロックする。
            // LoadTextAsync 内の plain await はこのコンテキストへ戻ろうとするが、
            // スレッドは .Result で塞がっているため、継続が走れず完了しない。
            string text = LoadTextAsync().Result;
            completed.TrySetResult();
        }, null);

        Task finished = await Task.WhenAny(completed.Task, Task.Delay(TimeSpan.FromSeconds(2)));

        Assert.NotSame(completed.Task, finished);

        // 注意: 模擬 UI スレッドは .Result で永久に塞がったままになる。
        // バックグラウンドスレッドなのでテストプロセスの終了は妨げない。
        static async Task<string> LoadTextAsync()
        {
            await Task.Delay(50); // plain await: 現在の SynchronizationContext を捕まえる
            return "done";
        }
    }
}
