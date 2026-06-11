# C#（CSharp）でPowerShellを実行して、オブジェクトとして受け取る方法 ── サンプルコード

ブログ記事「[C#（CSharp）でPowerShellを実行して、オブジェクトとして受け取る方法](https://comcomponent.com/blog/2026/06/08/001-csharp-run-powershell-receive-objects/)」のサンプルコードです。

C# から PowerShell を実行するとき、外部プロセスとして起動して標準出力を文字列で読むのではなく、PowerShell SDK（`Microsoft.PowerShell.SDK`）を使うと、結果を `Collection<PSObject>` として受け取れます。`BaseObject` や `Properties` から構造を保ったまま値を取り出せるため、文字列のパースが不要になります。

このサンプルでは、記事で紹介している **`AddCommand` / `AddParameter` によるパイプラインの組み立て**、**`PSObject` から C# の record への変換**、**エラーストリームの回収**、**小さな実行ラッパー**の実装と、その検証コードを提供します。

## 構成

```
csharp-run-powershell-receive-objects/
├── src/KomuraSoft.PowerShellObjects/          PowerShell 連携の実装（クラスライブラリ）
│   ├── ProcessSummary.cs                      PSObject から変換する C# 側の record（記事 6 章）
│   ├── ProcessSummaryMapper.cs                Properties から列名で値を取り出す変換処理（記事 6 章）
│   └── PowerShellRunner.cs                    出力とエラーをまとめて返す実行ラッパー（記事 11 章）
├── samples/Demo/                              記事の各章のコードを順番に実行するコンソールアプリ
└── tests/KomuraSoft.PowerShellObjects.Tests/  BaseObject / Properties / エラー処理を検証するユニットテスト
```

## 必要環境

- [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0) 以降

PowerShell 本体のインストールは不要です。`Microsoft.PowerShell.SDK`（7.4 系）が NuGet パッケージとして PowerShell エンジンをアプリに取り込むため、Windows / Linux / macOS のいずれでも動作します。

## 実行方法

デモ（PSObject の受け取り、Select-Object、record への変換、PSCustomObject、エラー処理、実行ラッパーを順番に実演します）:

```console
dotnet run --project samples/Demo
```

テスト（BaseObject の型、Select-Object 結果の Properties 読み取り、record への変換、エラーストリームの回収、ErrorAction Stop の例外化などを検証します）:

```console
dotnet test
```

## ポイント

- `Invoke()` の戻り値は文字列ではなく `Collection<PSObject>`。表示形式ではなくデータ構造に基づいて値を取り出せる
- コマンドをそのまま実行した結果は `BaseObject`、`Select-Object` や `[pscustomobject]` で整形した結果は `Properties["列名"]` で読む
- C# で後続処理に使うなら `Format-Table` ではなく `Select-Object` / `[pscustomobject]` を使う
- ユーザー入力を `AddScript` の文字列へ直接埋め込まず、`AddCommand` / `AddParameter` で値として渡す
- 出力とエラーは別ストリーム。`HadErrors` と `Streams.Error` を確認するか、`ErrorAction Stop` で例外として扱う
- `PSObject` は PowerShell との境界で扱い、アプリ内部では `ProcessSummary` のような C# の型に変換する

## 記事からの変更点（クロスプラットフォーム対応）

このサンプルは Windows 以外でも動作確認できるように、記事中の Windows 固有の例を次のように置き換えています。

- `Get-Service`（Windows 固有）を使う例（記事 11 章の実行ラッパーの使用例）→ `Get-Process` のパイプラインに置き換え
- `C:\no-such-file.txt`（記事 10 章のエラー処理の例）→ 一時フォルダー配下の存在しないファイルパスに置き換え
- `CPU` のような ScriptProperty の値は、環境によって `PSObject` に包まれたまま返ることがあるため、`Convert.ToDouble` に渡す前に `BaseObject` を取り出す処理を追加（記事 5 章・6 章のコードへの追記）

`AddCommand` / `AddParameter` / `Invoke` / `PSObject` の扱い方そのものは記事のコードと同じです。

詳しい解説は[記事本文](https://comcomponent.com/blog/2026/06/08/001-csharp-run-powershell-receive-objects/)をご覧ください。
