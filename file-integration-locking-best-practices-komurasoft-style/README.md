# ファイル連携の排他制御の基礎知識 ── サンプルコード

ブログ記事「[ファイル連携の排他制御の基礎知識 - ファイルロックと原子的 claim のベストプラクティス](https://comcomponent.com/blog/2026/03/07/001-file-integration-locking-best-practices-komurasoft-style/)」のサンプルコードです。

ファイル連携の排他制御は、ロック関数を 1 つ呼べば終わりではなく、「いつ読んでよいのか」「誰が処理権を持つのか」「失敗したときにどう回復するのか」を決める**受け渡しプロトコル**の設計が本体です。

このサンプルでは、記事で紹介している **`temp -> close -> rename / replace` による原子的公開**、**done / manifest による完全性の明示**、**rename による原子的 claim**、**lease 方式の lock file**、**idempotency（処理済み台帳）** の実装と、その検証コードを提供します。

## 構成

```
file-integration-locking-best-practices-komurasoft-style/
├── src/KomuraSoft.FileIntegration/         排他制御の実装（クラスライブラリ）
│   ├── AtomicFilePublisher.cs              temp -> close -> rename / replace（記事 4.1 / 5.2 章）
│   ├── TransferManifest.cs                 done / manifest と検証（記事 4.2 / 5.2 章）
│   ├── FileClaimer.cs                      incoming -> processing の claim rename（記事 4.3 / 5.2 章）
│   ├── LeaseLockFile.cs                    有効期限付き lock file（記事 4.4 章）
│   ├── ProcessedLedger.cs                  idempotency の処理済み台帳（記事 4.5 / 5.2 章）
│   ├── FileRetry.cs                        一時的な IOException へのリトライ
│   ├── FileTransferSender.cs               送信側の手順（記事 5.2 章）
│   └── FileTransferReceiver.cs             受信側の手順（記事 5.2 章）
├── samples/Demo/                           2 ワーカーの claim 競合と lease 引き取りの実演コンソールアプリ
└── tests/KomuraSoft.FileIntegration.Tests/ 競合・破損・stale lock を再現するユニットテスト
```

## 必要環境

- [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0) 以降

## 実行方法

デモ（一時ディレクトリ上で、原子的公開 → 2 ワーカーの claim 競合 → 重複 key の skip → lease lock の引き取りを実演します）:

```console
dotnet run --project samples/Demo
```

テスト（書き込み途中に final 名が見えないこと、16 並列の claim で勝者が 1 人だけになること、stale lease の引き取り、ハッシュ不一致の検出、FileShare.None と競合した読み取りのリトライなどを検証します）:

```console
dotnet test
```

## ポイント

- 生成中のファイルは temp 名に閉じ込め、flush / close してから final 名へ rename / replace する（final 名が見えた時点で「読んでよい」）
- temp と final は同一ディレクトリ（同一ボリューム / ファイルシステム）に置く
- 完了はサイズ安定待ちで推測せず、done / manifest（ファイル名・サイズ・ハッシュ・idempotency key）で明示する。done を置くのは本体の公開より「あと」
- 複数ワーカーがいるなら、読む前に `incoming -> processing` の rename で claim を原子的に取る。rename に成功したワーカーだけが処理する
- `Exists -> Create` の二段階チェックはせず、`FileMode.CreateNew` で「無ければ作る」を 1 操作にする
- lock file は空ファイルではなく、所有者・有効期限・heartbeat を持つ lease にする。期限切れの lock は後続が引き取れるようにし、削除は原則として作成者だけが行う
- 排他が一度破れても壊れないよう、idempotency key と処理済み台帳で二重実行を受け止める

詳しい解説は[記事本文](https://comcomponent.com/blog/2026/03/07/001-file-integration-locking-best-practices-komurasoft-style/)をご覧ください。
