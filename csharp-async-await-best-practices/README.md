# C# async/await実務判断表 ── サンプルコード

ブログ記事「[C# async/await実務判断表 - Task.RunとConfigureAwait](https://comcomponent.com/blog/2026/03/09/001-csharp-async-await-best-practices/)」のサンプルコードです。

`async` / `await` で実務上迷いやすいのは、構文そのものより「どの場面でどの書き方を選ぶべきか」です。このサンプルでは、記事の判断表に登場する各パターン（plain `await`、`Task.Run`、`Task.WhenAll` / `Task.WhenAny`、`Parallel.ForEachAsync`、`Channel<T>`、`PeriodicTimer`、`IAsyncEnumerable<T>`、`await using`、`SemaphoreSlim`）を、ビルド・実行・テストできる形で提供します。

## 構成

```
csharp-async-await-best-practices/
├── src/KomuraSoft.AsyncPatterns/            各パターンの実装（クラスライブラリ）
│   ├── FileTextLoader.cs                    I/O 待ちは async API をそのまま await（記事 3.2）
│   ├── CpuBoundHasher.cs                    重い CPU 計算を Task.Run で外す（記事 3.3）
│   ├── HttpDownloader.cs                    WhenAll / WhenAny / Parallel.ForEachAsync / token の伝播（記事 3.4〜3.6、4.3）
│   ├── BackgroundTaskQueue.cs               Channel<T> と backpressure（記事 3.7）
│   ├── PeriodicCacheRefresher.cs            PeriodicTimer で一定間隔（記事 3.8）
│   ├── UserStreamProcessor.cs               IAsyncEnumerable<T> と LINQ の ToArray 確定（記事 3.9、4.5）
│   ├── AsyncFileWriter.cs                   await using による非同期破棄（記事 3.10）
│   ├── CacheRefresher.cs                    await をまたぐ排他は SemaphoreSlim（記事 3.11）
│   ├── CounterStore.cs                      Task.CompletedTask / Task.FromResult（記事 4.1）
│   ├── UiEventHandlerNotes.cs               async void イベントハンドラ（WinForms 依存のためコメント収録、記事 4.2）
│   └── AntiPatterns.cs                      「良くない例」の収録（記事 3.2、3.4、5 章）
├── samples/Demo/                            各パターンを順番に実演するコンソールアプリ（HTTP はスタブで代用）
└── tests/KomuraSoft.AsyncPatterns.Tests/    並行性・直列性・backpressure・キャンセル伝播を検証するユニットテスト
```

## 必要環境

- [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0) 以降

## 実行方法

デモ（判断表の各パターンを、外部ネットワークに出ない形で順番に実演します）:

```console
dotnet run --project samples/Demo
```

テスト（WhenAll の並行開始、WhenAny の敗者回収、並列数の上限、Channel の backpressure、SemaphoreSlim による直列化、キャンセルの伝播などを検証します）:

```console
dotnet test
```

## ポイント

- まず処理が I/O 待ちか CPU 計算かを分ける。I/O 待ちなら async API をそのまま `await` し、`Task.Run` で包まない
- 独立した複数処理は、`ToArray()` で全タスクを開始してから `Task.WhenAll` でまとめて待つ（LINQ の遅延実行に注意）
- `Task.WhenAny` は勝者を返すだけなので、残りのキャンセルと例外の回収まで書く
- 件数が多いときは `Parallel.ForEachAsync` の `MaxDegreeOfParallelism` で並列数の上限を明示する
- fire-and-forget は素の `Task.Run` ではなく、境界付きの `Channel<T>` に積んでコンシューマが順番に処理する
- `await` をまたぐ排他は `SemaphoreSlim.WaitAsync` を使い、`Release` は `finally` で必ず呼ぶ
- `CancellationToken` は受けたらそのまま下流へ渡す
- 返り値はまず `Task` / `Task<T>`。`await` する処理がないなら `Task.CompletedTask` / `Task.FromResult` を返す

記事 4.2 の `async void` イベントハンドラの例は WinForms 依存（`MessageBox` / `Label`）のため、ビルド対象にせず `UiEventHandlerNotes.cs` にコメントとして収録しています。また、記事 5 章のアンチパターンのうち、実行するとスレッドを塞ぐもの（`Task.Result` / `Wait()` など）はコードとして収録せず、安全に実行できる「良くない例」だけを `AntiPatterns.cs` に収録しています。

詳しい解説は[記事本文](https://comcomponent.com/blog/2026/03/09/001-csharp-async-await-best-practices/)をご覧ください。
