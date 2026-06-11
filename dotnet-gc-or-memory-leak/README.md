# .NETでGC待ちとメモリリークを見分ける ── サンプルコード

ブログ記事「[.NETでGC待ちとメモリリークを見分ける ── 増えるメモリを観測・比較・証明する実務手順](https://comcomponent.com/blog/2026/06/09/000-dotnet-gc-or-memory-leak/)」のサンプルコードです。

.NET では、プロセスのメモリが増えていることと、メモリリークしていることは同じではありません。見るべきなのは「GC 後も生き残るメモリが増えているか」「増えている型は何か」「そのオブジェクトを誰が参照しているか」の 3 つです。

このサンプルでは、記事で紹介している**典型的なリーク（意図しない保持）のパターン**（static コレクション、無制限キャッシュ、イベント購読解除漏れ、Timer 破棄漏れ、DI ライフタイムの取り違えなど）と、「GC 待ち」との違いを実際に観測できる検証コードを提供します。

## 構成

```
dotnet-gc-or-memory-leak/
├── src/KomuraSoft.MemoryDiagnostics/        リークパターンと診断ヘルパー（クラスライブラリ）
│   ├── DiagnosticGc.cs                      調査専用のフル GC 誘発（記事 7 章）
│   ├── GcDiagnostics.cs                     GC 情報のスナップショット（記事 17 章）
│   ├── RequestContext.cs                    AsyncLocal によるコンテキスト保持（記事 11.6 章）
│   ├── PooledBuffers.cs                     ArrayPool の Rent / Return（記事 12 章）
│   ├── Leaky/                               リークする実装（記事 11 章の「悪い例」）
│   │   ├── CustomerStore.cs                 static コレクション（記事 11.1 章）
│   │   ├── ReportCache.cs                   上限も期限もないキャッシュ（記事 11.2 章）
│   │   ├── OrderViewModel.cs                イベント購読解除漏れ（記事 11.3 章）
│   │   ├── PollingWorker.cs                 Timer の破棄漏れ（記事 11.4 章）
│   │   ├── FileTextReader.cs                所有権が曖昧な IDisposable（記事 11.5 章）
│   │   └── AuditBuffer.cs                   DI ライフタイムの取り違え（記事 11.7 章）
│   └── Fixed/                               修正版の実装（購読解除・Dispose・await using）
├── samples/Demo/                            「GC 待ち」と「生き残り」の違いを観測するコンソールアプリ
└── tests/KomuraSoft.MemoryDiagnostics.Tests/ WeakReference とフル GC で保持・回収を検証するユニットテスト
```

## 必要環境

- [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0) 以降
- 観測手順を試す場合は [dotnet-counters / dotnet-gcdump / dotnet-dump](https://learn.microsoft.com/en-us/dotnet/core/diagnostics/tools-overview)

## 実行方法

デモ（一時割り当てが GC で戻ること、static 保持・イベント・Timer で生き残ることを順に表示します）:

```console
dotnet run --project samples/Demo
```

テスト（static コレクション・イベント・Timer による保持と Dispose 後の回収、無制限キャッシュの増加、AsyncLocal のフロー、ArrayPool の返却などを検証します）:

```console
dotnet test
```

## 観測手順（dotnet-counters / dotnet-gcdump / dotnet-dump）

リークモードでデモを起動すると、static コレクションへ追加し続けるプロセスになります。

```console
dotnet run --project samples/Demo -- --leak
```

別ターミナルから、記事の手順どおりに観測できます。表示された PID を `<PID>` に読み替えてください。

まず傾向を見ます（記事 6 章）。GC Heap と Gen 2 の底が上がり続けることが確認できます。

```console
dotnet tool install --global dotnet-counters
dotnet-counters monitor --process-id <PID> --refresh-interval 3 --counters System.Runtime
```

次に時間差で GC dump を取り、型ごとの Count / Size を比較します（記事 8 章）。`Customer` と `System.Byte[]` が増え続けることが確認できます。

```console
dotnet tool install --global dotnet-gcdump
dotnet-gcdump collect --process-id <PID> --output before.gcdump
# 1〜2 分待つ
dotnet-gcdump collect --process-id <PID> --output after.gcdump
dotnet-gcdump report before.gcdump > before-heap.txt
dotnet-gcdump report after.gcdump  > after-heap.txt
```

最後にダンプを取り、`dumpheap` と `gcroot` で参照元を追います（記事 9 章）。static な `CustomerStore` のリストから参照されていることが確認できます。

```console
dotnet tool install --global dotnet-dump
dotnet-dump collect --process-id <PID> --type Heap --output leak.dmp
dotnet-dump analyze leak.dmp
> dumpheap -stat -type KomuraSoft.MemoryDiagnostics.Customer
> dumpheap -mt <MT>
> gcroot <OBJECT_ADDRESS>
```

## ポイント

- Working Set や Total Allocated が増えているだけではリークとは言えない（記事 2〜3 章）
- 「GC 後も生き残る量が増えているか」を、同じ条件の時間差比較で見る（記事 4 章）
- 調査用のフル GC（`ForceFullGcForDiagnosticsOnly`）は切り分けのための道具であり、解決策ではない（記事 7・14 章)
- リークの正体は「参照の残り」── static、イベント、Timer、キャッシュ、DI ライフタイムが典型（記事 11 章）
- イベントは publisher と subscriber の寿命差に注意し、購読解除を Dispose に対応させる
- キャッシュは「増えてよいか」ではなく「どこまで増えてよいか」（上限・期限・削除条件）を決める
- `dumpheap -stat` は「何が多いか」、`gcroot` は「なぜ残っているか」── 修正につながるのは後者（記事 9 章）

詳しい解説は[記事本文](https://comcomponent.com/blog/2026/06/09/000-dotnet-gc-or-memory-leak/)をご覧ください。
