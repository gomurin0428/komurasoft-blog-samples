# Ada の検査ロジックを C# の検査アプリから呼ぶ ── サンプルコード

ブログ記事「[Adaの検査ロジックをC#の検査アプリから呼ぶ ── 画像バッファをコピーせずに渡し、境界で守る](https://comcomponent.com/blog/ada-inspection-dll-csharp/)」のサンプルコードです。

暗い欠陥の画素を数える検査ロジックを Ada（中心部は SPARK）で書き、C の呼び出し規約の関数として DLL（Linux では共有ライブラリ）にします。C# からは `LibraryImport` で画像のバッファをコピーせずに渡します。境界の関数はポインター・寸法・ストライド・パラメーターを確かめて状態コードを返し、例外を外に出しません。記事に載せた Ada・C・C# のコードブロックは、すべてこのフォルダのファイルにそのまま含まれています。

## 構成

```
ada-inspection-dll-csharp/
├── src/
│   ├── inspect_core.ad[sb]   検査の中心（SPARK、GNATprove で証明）
│   └── inspect_api.ad[sb]    C から呼ぶ境界の関数（入力を確かめて状態コードを返す）
├── c/
│   ├── test_inspect.c        C のテストドライバー
│   ├── probe_signals.c       ライブラリを読み込むとシグナルハンドラーが置き換わるかを見る
│   └── probe_bad_pointer.c   読めないアドレスを渡したときに返る状態コードを見る
├── cs/
│   ├── Shared/AdaInspector.cs   LibraryImport の宣言と、Span を受ける呼び出し
│   ├── InspectConsole/          .NET 10 のコンソールアプリ（テストと追加の確かめ）
│   └── InspectWpf/              WPF の検査画面の例（Task.Run で呼ぶ、状態99のあとは検査を止める）
├── nss/nss_violation.adb     No_Secondary_Stack でエラーになる例（文字列の連結を返す関数）
├── inspect.gpr               ライブラリのプロジェクト
├── inspect_nss.gpr           gnat.adc の No_Secondary_Stack を付けて同じライブラリをビルドする
├── gnat.adc                  pragma Restrictions (No_Secondary_Stack);
├── alire.toml
├── verify.sh                 Linux: ビルド、C のテスト、GNATprove、.NET のテスト
└── verify.ps1                Windows: DLL のビルド、.NET 10 のコンソールで呼ぶ、WPF のビルド
```

## 試し方

### 前提

- GNAT と gprbuild（Linux なら `apt install gnat gprbuild`、Windows なら [Alire](https://alire.ada.dev/)）
- [.NET 10 SDK](https://dotnet.microsoft.com/download/dotnet/10.0)
- 証明には GNATprove が必要です。Alire を使うと `alr` が GNAT・gprbuild・GNATprove をまとめて用意します。

### Linux

```sh
./verify.sh
```

共有ライブラリ `lib/libinspect.so` を作り、C のテストドライバーと .NET のコンソールアプリから呼びます。WPF の例は `-p:EnableWindowsTargeting=true` でコンパイルだけ確かめます。`alr` があれば GNATprove で `Inspect_Core` を証明します。

### Windows

```powershell
pwsh -NoProfile -File .\verify.ps1
```

`alr build` で `lib\libinspect.dll` を作り、GNAT のランタイムの DLL と一緒にコンソールアプリの出力フォルダーに置いて実行します。ランタイムの DLL を置かないと `DllNotFoundException` になることも確かめます。WPF の例はビルドだけ行います。

## 検証状態について

- **ローカル（Ubuntu 24.04、GNAT 13.3.0、gprbuild、.NET SDK 10.0.114）**: `verify.sh` の C のテスト、No_Secondary_Stack を付けたビルド、.NET のコンソールアプリのテスト、WPF のコンパイルが成功することを確認済みです。
- **GitHub Actions**: [verify-ada-machine-vision](../.github/workflows/verify-ada-machine-vision.yml) のワークフローで、ubuntu-24.04 では `verify.sh`（GNATprove を含む。記事を書いた時点では 20 件の検査がすべて証明）、windows-latest では `verify.ps1`（Alire の GNAT 16.1.0 と .NET 10）を実行します。
- WPF の画面での動作、32ビットの Windows、実際のカメラの SDK が渡すバッファ、複数のスレッドから同時に呼んだときの動作は確かめていません。Linux の .NET から呼ぶ使い方は記事の対象外で、`verify.sh` では動作の記録として実行しています。

## ポイント

- 画像のバッファは `ReadOnlySpan<byte>` を `fixed` で固定してポインターで渡し、長さも一緒に渡す
- 境界の関数は寸法・ストライド・長さを確かめてから中心部を呼び、例外を外に出さず状態コードで返す
- 中心部を SPARK にすると、境界で確かめた前提のもとで実行時エラーがないことを証明できる
- 状態99（内部エラー）が返ったら、それ以上その DLL で検査を続けない

詳しい解説は[記事本文](https://comcomponent.com/blog/ada-inspection-dll-csharp/)をご覧ください。
