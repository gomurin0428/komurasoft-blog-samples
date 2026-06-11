# .NET 8 DLLをVBAから型付きで使う方法 ── サンプルコード

ブログ記事「[.NET 8 DLLをVBAから型付きで使う方法 - COM公開とdscom TLB](https://comcomponent.com/blog/2026/03/16/007-dotnet8-dll-typed-vba-com-dscom-tlb/)」のサンプルコードです。

.NET 8 のクラスライブラリを `EnableComHosting=true` で COM 公開し、dscom でタイプライブラリ（TLB）を生成して、VBA から早期バインディングで型付き利用する構成のサンプルです。COM の起動入口は .NET SDK が作る `*.comhost.dll`、型情報は dscom が作る `*.tlb`、VBA はその TLB を参照設定して早期バインディングします。

## 構成

```
dotnet8-dll-typed-vba-com-dscom-tlb/
├── src/VbaTypedComSample/               COM 公開する .NET 8 クラスライブラリ
│   ├── VbaTypedComSample.csproj         EnableComHosting=true の csproj（記事 4.1）
│   ├── AssemblyInfo.cs                  アセンブリ全体を ComVisible(false) にする（記事 4.2）
│   └── Calculator.cs                    ICalculator / Calculator（記事 4.3）
├── scripts/                             Windows で実行する PowerShell スクリプト
│   ├── Install-Dscom.ps1                dscom のインストール（記事 6.1）
│   ├── Export-Tlb.ps1                   ビルドと dscom tlbexport（記事 5 章・6 章）
│   └── Register-ComServer.ps1           regsvr32 + dscom tlbregister（記事 7 章。要管理者権限）
├── vba/                                 参照用の VBA コード
│   ├── CalculatorBasicUsage.bas         型付きで Add / Divide / Hello を呼ぶ（記事 8 章）
│   └── CalculatorErrorHandling.bas      .NET の例外を COM エラーとして受ける（記事 8.1）
└── tests/
    ├── VbaTypedComSample.Tests/         COM に依存しない純粋ロジックの xUnit テスト
    └── Scripts.Tests.ps1                scripts/ の Pester 構文解析・ガード動作テスト
```

## 必要環境

- Windows（TLB 生成・COM 登録・VBA からの呼び出し）
- 64bit Office または 32bit Office（Excel / Access）
- [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0) 以降（ビルドは Linux でも可。csproj の `<EnableWindowsTargeting>true</EnableWindowsTargeting>` により `net8.0-windows` を Linux 上でもビルドできます）
- [dscom](https://www.nuget.org/packages/dscom)（Windows 上で `scripts/Install-Dscom.ps1` でインストール）

このサンプルは COM ホスティング・dscom・VBA という Windows 専用の仕組みを扱うため、作成環境（Linux）では **ビルド（`*.comhost.dll` の生成を含む）と構文検証のみ検証済み** です。TLB 生成と VBA からの呼び出しは Windows 上で実施してください。

## Linux / 任意の環境でできること

ビルド（`VbaTypedComSample.comhost.dll` が生成されることを確認できます）:

```console
dotnet build -c Release
```

テスト（`Add` / `Divide` / `Hello` のロジックと、GUID・`InterfaceIsDual`・`ClassInterfaceType.None` という COM 契約のメタデータを検証します。COM 登録は不要で、Linux でも実行できます）:

```console
dotnet test
```

スクリプトの静的検証（パースエラーがないことと、非 Windows では警告を出して終了することを確認します）:

```console
pwsh -Command "Invoke-Pester -Path ./tests/Scripts.Tests.ps1"
```

## Windows での手順（記事の流れ）

bitness を最初に決めてください。Office / VBA と COM サーバーの bitness は揃える必要があります（記事 3 章）。以下は 64bit Office 向けです。32bit Office なら各スクリプトに `-Bitness x86` を付け、`tools\dscom32.exe` を配置しておいてください。

1. dscom をインストールする（記事 6.1）

   ```powershell
   .\scripts\Install-Dscom.ps1
   ```

2. Release ビルドし、TLB を生成する（記事 5 章・6 章）

   ```powershell
   .\scripts\Export-Tlb.ps1
   ```

   `src\VbaTypedComSample\bin\Release\net8.0-windows\` に `VbaTypedComSample.dll` / `VbaTypedComSample.comhost.dll` / `VbaTypedComSample.tlb` などが揃います。

3. **管理者権限の PowerShell** で COM host と TLB を登録する（記事 7 章）

   ```powershell
   .\scripts\Register-ComServer.ps1
   ```

   `regsvr32` で `*.comhost.dll` を COM サーバーとして、`dscom tlbregister` で `*.tlb` をタイプライブラリとして登録します。配置場所を変えたら登録もやり直しになります。

4. VBA で参照設定して使う（記事 8 章）

   1. Excel または Access の VBA エディタで `ツール` -> `参照設定` を開く
   2. 一覧にライブラリが出ていればチェック、見えなければ `参照...` から `VbaTypedComSample.tlb` を選ぶ
   3. `vba/CalculatorBasicUsage.bas` と `vba/CalculatorErrorHandling.bas` を標準モジュールとして取り込み、実行する

   ```vb
   Dim calc As VbaTypedComSample.ICalculator
   Set calc = New VbaTypedComSample.Calculator
   Debug.Print calc.Add(10, 20)
   ```

   IntelliSense が効き、メソッド名の typo が実行前に見つけやすくなります。`Divide(10, 0)` のように .NET 側で例外が投げられると、VBA 側では COM エラー（`Err.Number` / `Err.Description`）として見えます。

## ポイント

- VBA が型を知るために必要なのは TLB、COM の起動入口として必要なのは comhost。`.dll` 単体を渡して終わりではない
- Office / VBA と COM サーバーの bitness を揃える。`AnyCPU` のまま放置せず、64bit Office なら `x64` / `win-x64` を明示する
- COM に見せる明示的なインターフェイスを定義し、クラスは `ClassInterfaceType.None` にして `AutoDual` に逃げない
- VBA から使うインターフェイスは `InterfaceIsDual` にし、`DispId` を振っておく
- GUID（IID / CLSID）は契約そのもの。公開後に軽率に再生成しない
- 公開済みインターフェイスは壊さない。変更が大きいなら `ICalculator2` を新設する
- 配布時は DLL 単体ではなく出力一式（`*.dll` / `*.comhost.dll` / `*.deps.json` / `*.runtimeconfig.json` / `*.tlb`）を置き、クライアント PC には対応する .NET 8 ランタイムを入れる
- Office を開いたまま更新しない（DLL を掴んだままになり、ビルドや再登録で面倒が起きる）

詳しい解説は[記事本文](https://comcomponent.com/blog/2026/03/16/007-dotnet8-dll-typed-vba-com-dscom-tlb/)をご覧ください。
