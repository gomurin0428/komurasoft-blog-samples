# C#からネイティブDLLを呼ぶ：C++/CLIラッパー vs P/Invoke ── サンプルコード

ブログ記事「[C#からネイティブDLLを呼ぶ：C++/CLIラッパー vs P/Invoke](https://comcomponent.com/blog/2026/03/07/000-cpp-cli-wrapper-for-native-dlls/)」のサンプルコードです。

C++ のクラス・所有権・`std::wstring`・`std::vector`・例外が絡むネイティブライブラリを C# から使うとき、P/Invoke で押し切る場合と C++/CLI で薄いラッパーを挟む場合とで、境界面がどう変わるかを比較できる構成にしています。

記事のコード抜粋（ネイティブ API、C API ブリッジ、P/Invoke 宣言、C++/CLI ラッパー、C# 消費コード）をそのまま収録し、記事で省略されている実装（`Analyzer` のダミー実装、ブリッジの構造体定義と実装など）をサンプル側で補っています。

## 構成

```
cpp-cli-wrapper-for-native-dlls/
├── CppCliWrapperSample.sln            Visual Studio 用ソリューション（Windows）
├── src/native/                        ネイティブ C++ ライブラリ
│   ├── NativeLib.hpp                  ネイティブ DLL 側の API イメージ（記事 6.1）
│   ├── NativeLib.cpp                  Analyzer のダミー実装（サンプルで補完）
│   ├── NativeBridge.hpp / .cpp        P/Invoke 用の C API ブリッジ（記事 6.2）
│   └── NativeLib.vcxproj / NativeBridge.vcxproj
├── src/wrapper/                       C++/CLI ラッパー（Windows + MSVC 専用）
│   ├── AnalyzerWrapper.h / .cpp       ラッパーの宣言と実装（記事 6.3）
│   └── AnalyzerWrapper.vcxproj        /clr:netcore（.NET 8 向け）
├── src/consumer/                      C# 消費側
│   ├── WrapperConsumer/               C++/CLI ラッパー経由（記事 6.3 の C# コード）
│   └── PInvokeConsumer/               P/Invoke 直接（記事 6.2 の C# コード）
└── tests/native-smoke/                ネイティブ部分のスモークテスト（g++ / MSVC で実行可）
```

## 必要環境

フルビルド（C++/CLI を含む）は Windows 専用です。

- Windows + Visual Studio 2022
  - ワークロード「C++ によるデスクトップ開発」
  - 個別コンポーネント「**C++/CLI support for v143 build tools (Latest)**」（既定ではインストールされません）
- [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0) 以降

ネイティブ C++ 部分（`src/native/`）と P/Invoke 側 C# プロジェクトだけなら、Linux の g++ / .NET 8 SDK でも確認できます（後述）。

## ビルドと実行（Windows）

`CppCliWrapperSample.sln` を Visual Studio で開いてビルドするか、Developer Command Prompt で次の順にビルドします（構成は `Debug` / `Release`、プラットフォームは `x64`）。

```console
msbuild src\native\NativeLib.vcxproj /p:Configuration=Debug /p:Platform=x64 /restore
msbuild src\native\NativeBridge.vcxproj /p:Configuration=Debug /p:Platform=x64 /restore
msbuild src\wrapper\AnalyzerWrapper.vcxproj /p:Configuration=Debug /p:Platform=x64 /restore
```

C++/CLI ラッパー経由の消費側（記事の本命の構成）:

```console
dotnet run --project src\consumer\WrapperConsumer
```

P/Invoke 直接の消費側（比較用）:

```console
dotnet run --project src\consumer\PInvokeConsumer
```

どちらも同じネイティブ実装を呼びますが、C# 側のコード量と「境界面の都合がどこまで漏れてくるか」が大きく違います。2 つの `Program.cs` を見比べてみてください。

## Linux で検証できる範囲

ネイティブ部分はプレーンな C++ なので、g++ でスモークテストを実行できます。

```console
g++ -std=c++17 -Wall -Wextra -I src/native tests/native-smoke/main.cpp src/native/NativeLib.cpp src/native/NativeBridge.cpp -o native-smoke
./native-smoke
```

P/Invoke 側の C# プロジェクトは、Linux でもビルドだけは確認できます（実行には Windows でビルドした `NativeBridge.dll` が必要です）。

```console
dotnet build src/consumer/PInvokeConsumer
```

## 検証状態

このサンプルは Linux 環境で作成しているため、検証できた範囲を正直に書いておきます。

| 対象 | 状態 |
| --- | --- |
| `src/native/`（ネイティブ C++） | g++ 13 で共有ライブラリとしてコンパイル確認、スモークテスト全項目パス。MinGW-w64（x86_64-w64-mingw32-g++）で Windows 向け DLL（PE32+）の生成も確認 |
| `src/consumer/PInvokeConsumer`（C#） | .NET 8 SDK で `dotnet build` 成功 |
| `src/wrapper/`（C++/CLI） | **未検証**（C++/CLI は Windows + MSVC 専用のため。コードは記事掲載のものと同一） |
| `src/consumer/WrapperConsumer`（C#） | **未検証**（Windows でビルドした `AnalyzerWrapper.dll` への参照が必要なため） |
| `*.vcxproj` / `.sln` | **未検証**（MSVC が必要なため。Visual Studio 2022 の標準的な設定で記述） |

C++/CLI 部分のビルドで問題が出る場合は、Visual Studio Installer で「C++/CLI support for v143 build tools」がインストールされているか、`WrapperConsumer` 実行時に `Ijwhost.dll` が出力フォルダーにコピーされているかを確認してください。

## ポイント

- 相手が C の関数群なら P/Invoke が素直、相手が C++ のライブラリなら C++/CLI ラッパーを 1 枚挟むと保守しやすい
- P/Invoke で C++ クラスを相手にすると、結局 C API ブリッジ（`NativeBridge`）の設計を始めることになる
- C API 境界では、例外がエラーコードに潰れ、`std::vector` / `std::wstring` は固定長バッファとの格闘になる（`NativeBridge.cpp` 参照）
- C++/CLI 側ではネイティブのヘッダーをそのままインクルードでき、`marshal_as` で変換して `string` / `List<int>` / `IDisposable` / 例外という .NET らしい面だけを見せられる
- C++/CLI ラッパーはあくまで「翻訳」と「整形」に徹し、業務ロジックを入れない
- C++/CLI は Windows 前提。クロスプラットフォームが必要な場合や、相手が最初からきれいな C API の場合は選ばない

詳しい解説は[記事本文](https://comcomponent.com/blog/2026/03/07/000-cpp-cli-wrapper-for-native-dlls/)をご覧ください。
