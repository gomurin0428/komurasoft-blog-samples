# FileSystemWatcher実務ガイド ── サンプルコード

ブログ記事「[FileSystemWatcher実務ガイド - 取りこぼしと重複対策](https://comcomponent.com/blog/2026/03/10/000-filesystemwatcher-safe-basics/)」のサンプルコードです。

`FileSystemWatcher` のイベントは完了通知ではなく「変化の気配」です。`Created` / `Changed` は重複したり、想像と違う順序で来たり、内部バッファあふれ（overflow）時には取りこぼしたりします。

このサンプルでは、記事で紹介している「**通知は再スキャン要求に畳む → 再スキャンで ready を見つける → claim を原子的に取る → idempotency で受け止める**」という設計の実装と、一時ディレクトリ上で実際にファイルを作成・変更して検証するテストコードを提供します。

## 構成

```
filesystemwatcher-safe-basics/
├── src/KomuraSoft.FileWatching/            監視と bundle 処理の実装（クラスライブラリ）
│   ├── BundleProcessingWorker.cs           通知を scan request に畳む走査ワーカー（記事 4.1 / 4.4 / 5.2 節）
│   ├── BundleScanner.cs                    ディレクトリ再スキャンで ready な bundle を列挙（記事 4.1 節）
│   ├── AtomicClaim.cs                      incoming -> processing の claim rename（記事 4.3 節）
│   ├── BundleWriter.cs                     送信側の temp -> close -> rename + manifest（記事 4.2 節）
│   ├── BundleManifest.cs                   IdempotencyKey 入り manifest（記事 4.5 節）
│   ├── FileProcessedStore.cs               処理済み key の照合と記録（記事 4.5 節）
│   └── TypicalFailurePattern.cs            典型的な失敗パターンの比較用コード（記事 5.1 節）
├── samples/Demo/                           一時ディレクトリ上で startup scan・通知・重複投入を実演するコンソールアプリ
└── tests/KomuraSoft.FileWatching.Tests/    実ファイル操作で通知から archive までを検証するテスト
```

## 必要環境

- [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0) 以降

## 実行方法

デモ（一時ディレクトリ上で、起動前から置かれていた bundle の startup scan、起動後に投入した bundle の処理、idempotency key 重複のスキップを実演します）:

```console
dotnet run --project samples/Demo
```

テスト（startup full rescan、watcher 通知での処理、temp 名 bundle の除外、idempotency key 重複のスキップ、2 ワーカーでの exactly-once な claim などを、一時ディレクトリへの実ファイル作成・変更とポーリング + タイムアウトで検証します）:

```console
dotnet test
```

## ポイント

- イベントハンドラでは重い処理をせず、再スキャン要求を立ててすぐ返す（通知バーストは少しまとめてから 1 回走査する）
- 真実はイベント列ではなく、いまディスク上に見えている状態（startup / `Error` / 定期の full rescan で整合性を回復する）
- 完了は推測ではなく明示する（`temp` 名に全内容を書く → close → 同一ファイルシステム上で rename + manifest）
- 複数ワーカーがいるなら、読む前に `incoming -> processing/<worker>/` の rename で claim を原子的に取る
- exactly-once をイベントだけで作ろうとせず、at-least-once を受け入れて idempotency key で副作用の再実行を止める

詳しい解説は[記事本文](https://comcomponent.com/blog/2026/03/10/000-filesystemwatcher-safe-basics/)をご覧ください。
