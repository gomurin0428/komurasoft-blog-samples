# .NETタイマー3種の使い分け ── サンプルコード

ブログ記事「[.NETタイマー3種の使い分け - PeriodicTimer/Timer/DispatcherTimer](https://comcomponent.com/blog/2026/03/12/002-periodictimer-system-threading-timer-dispatchertimer-guide/)」のサンプルコードです。

`PeriodicTimer` / `System.Threading.Timer` / `DispatcherTimer` は名前こそ似ていますが、「`await` でティックを待つ」「ThreadPool で callback が飛んでくる」「UI スレッドの `Dispatcher` 上で動く」というように性格がかなり違います。このサンプルでは、記事の典型パターン（4 章）のコードと、誤解しやすい性質（tick の畳み込み、callback の重なり）を検証するテストを提供します。

## 構成

```
periodictimer-system-threading-timer-dispatchertimer-guide/
├── src/KomuraSoft.TimerSelection/              タイマーの典型パターン（クラスライブラリ）
│   ├── CacheRefreshWorker.cs                   PeriodicTimer による async な定期処理（記事 4.1）
│   └── HeartbeatService.cs                     System.Threading.Timer + 重複起動ガード（記事 4.2）
├── samples/Demo/                               2 つのサービスを汎用ホストで動かすコンソールアプリ
├── tests/KomuraSoft.TimerSelection.Tests/      タイマーの性質と各サービスの動作を検証するユニットテスト
│   ├── CacheRefreshWorkerTests.cs              起動直後の 1 回目の更新と、キャンセルによる停止
│   ├── HeartbeatServiceTests.cs                heartbeat の発火と Start / Stop / Dispose の流れ
│   ├── PeriodicTimerBehaviorTests.cs           tick の畳み込み・キャンセル・Dispose（記事 3.1、5.3）
│   └── ThreadPoolTimerOverlapTests.cs          callback の重なりと Interlocked ガード（記事 4.2）
└── docs/MainWindowClock.cs.txt                 DispatcherTimer による時計表示（記事 4.3、ビルド対象外）
```

`DispatcherTimer`（記事 4.3）は WPF 固有で、Windows 上の WPF プロジェクト（`net8.0-windows` + `<UseWPF>true</UseWPF>`）でのみビルドできます。このリポジトリは Linux でもビルド・テストできるようにしているため、`DispatcherTimer` のコードはビルド対象に含めず、`docs/MainWindowClock.cs.txt` に参照用として収録しています。

## 必要環境

- [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0) 以降

## 実行方法

デモ（`PeriodicTimer` の worker と `System.Threading.Timer` の heartbeat を同じホストで約 7 秒間動かします）:

```console
dotnet run --project samples/Demo
```

テスト（起動直後の更新、キャンセルによる停止、tick の畳み込み、callback の重なり、`Interlocked.Exchange` ガードの効果などを検証します）:

```console
dotnet test
```

タイマーのテストはタイミング依存になりやすいので、固定の待ち時間で判定せず「余裕のあるタイムアウト + 条件成立で即終了」のポーリングと、決定的に判定できる性質（ガードがあれば同時実行は常に 1、など）で検証しています。

## ポイント

- 最初に見るのは「どこで動くか」「`async` / `await` で直列に書きたいか」「callback の重なりを許せるか」の 3 つ
- async な定期処理なら `PeriodicTimer`。「待つ → 処理する → また待つ」を 1 本の async メソッドとして書けて、`CancellationToken` をそのまま下流へ渡せる
- `PeriodicTimer` は遅れを自動で取り戻さない。待っていない間の tick は 1 回に畳まれる（`PeriodicTimerBehaviorTests` で確認）
- `System.Threading.Timer` の callback は ThreadPool で動き、前回の完了を待たずに重なりうる。重い処理を入れるなら `Interlocked.Exchange` などでガードする（`ThreadPoolTimerOverlapTests` で確認）
- `System.Threading.Timer` は参照を保持しないと GC の対象になる。フィールドで持ち、`Change` / `Dispose` で停止と寿命を明示する
- `System.Threading.Timer` に `async` ラムダをそのまま渡すと実質 `async void` になる。処理本体が async なら `PeriodicTimer` に寄せる
- `DispatcherTimer` の Tick は UI スレッドで動くので UI をそのまま触れるが、重い処理を入れると入力・描画まで巻き込む。閉じるときは `Stop()` と購読解除を忘れない

詳しい解説は[記事本文](https://comcomponent.com/blog/2026/03/12/002-periodictimer-system-threading-timer-dispatchertimer-guide/)をご覧ください。
