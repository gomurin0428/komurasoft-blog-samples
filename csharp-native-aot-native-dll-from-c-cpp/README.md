# C# Native AOT DLLをC/C++から呼び出す方法 ── サンプルコード

ブログ記事「[C# Native AOT DLLをC/C++から呼び出す方法](https://comcomponent.com/blog/2026/03/12/003-csharp-native-aot-native-dll-from-c-cpp/)」のサンプルコードです。

.NET の Native AOT では、C# のクラスライブラリをネイティブ共有ライブラリ（Windows なら `.dll`、Linux なら `.so`）として発行でき、`UnmanagedCallersOnly` を付けたメソッドを C のエントリポイントとして公開できます。つまり、C# を「呼ばれる側のネイティブ DLL」として使えます。

このサンプルでは、記事の最小例である**加算器（accumulator）の C API**（`create` / `add` / `get_total` / `destroy` のフラットな関数 + handle + エラーコード）を提供します。

## 構成

```
csharp-native-aot-native-dll-from-c-cpp/
├── src/NativeAotSample/                  Native AOT で発行する C# ライブラリ
│   ├── NativeAotSample.csproj            PublishAot / AllowUnsafeBlocks の設定（記事 4.1 章）
│   ├── NativeExports.cs                  UnmanagedCallersOnly の export と内部ロジック（記事 4.2 章）
│   └── AssemblyInfo.cs                   テスト用の InternalsVisibleTo（記事には登場しない補助ファイル）
├── samples/cpp-caller/                   C++ 側の呼び出し例
│   ├── native_api.h                      C API の status とシグネチャ定義（記事 4.4 章）
│   ├── main.cpp                          Windows 版（LoadLibrary / GetProcAddress、記事 4.4 章）
│   └── main_linux.cpp                    Linux 版（dlopen / dlsym。記事のコードを POSIX に置き換えたもの）
└── tests/NativeAotSample.Tests/          export の中身のロジックを検証する xUnit テスト
    ├── AccumulatorStoreTests.cs          handle 管理・status code・並行 Add の検証
    └── NativeExportsTests.cs             unmanaged 関数ポインタ経由で export 層を検証
```

## 必要環境

- [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0) 以降
- Native AOT publish には、各 OS のネイティブツールチェーンが必要です
  - Windows: Visual Studio 2022 の「C++ によるデスクトップ開発」ワークロード
  - Linux: `clang` と `zlib1g-dev`（ディストリビューションにより名前は異なります）

## ビルドとテスト

ライブラリとテストのビルド（Native AOT のコンパイルは publish 時のみ走るため、ここはふつうの managed ビルドです）:

```console
dotnet build
```

テスト（handle の払い出し・破棄、不正 handle / null ポインタへの status code、複数スレッドからの `add` などを検証します）:

```console
dotnet test
```

`UnmanagedCallersOnly` のメソッドは managed コードから直接呼び出せません。記事 5.5 章の「export メソッドは薄くして本体を別に置く」分業のおかげで、本体の `AccumulatorStore` を通常の xUnit でテストできます。さらに `NativeExportsTests` では、`delegate* unmanaged[Cdecl]` の関数ポインタ経由で export 層（null チェックなど）も in-process で検証しています。

## ネイティブ DLL の発行と C++ からの呼び出し

### Windows（記事本文の手順）

DLL の発行:

```console
cd src/NativeAotSample
dotnet publish -r win-x64 -c Release /p:NativeLib=Shared
```

`bin/Release/net8.0/win-x64/publish/NativeAotSample.dll` が出力されます。

C++ 側のビルド（Developer Command Prompt for VS で）:

```console
cd samples\cpp-caller
cl /EHsc /W4 main.cpp /Fe:caller.exe
```

`NativeAotSample.dll` を `caller.exe` と同じフォルダにコピーして実行します:

```console
copy ..\..\src\NativeAotSample\bin\Release\net8.0\win-x64\publish\NativeAotSample.dll .
caller.exe
```

```
total = 30
```

### Linux

`.so` の発行:

```console
cd src/NativeAotSample
dotnet publish -r linux-x64 -c Release /p:NativeLib=Shared
```

`bin/Release/net8.0/linux-x64/publish/NativeAotSample.so` が出力されます。

C++ 側のビルドと実行（`gcc` / `g++` の例）:

```console
cd samples/cpp-caller
g++ -O2 -o caller main_linux.cpp -ldl
./caller ../../src/NativeAotSample/bin/Release/net8.0/linux-x64/publish/NativeAotSample.so
```

```
total = 30
```

`main_linux.cpp` は、記事の Windows 版 `main.cpp` の `LoadLibraryW` / `GetProcAddress` を `dlopen` / `dlsym` に置き換えたものです。`native_api.h` は両 OS で共用しています（`__cdecl` は MSVC 拡張なので、Linux 側では空定義にしています）。

このリポジトリのサンプルは、Linux x64 上で「`dotnet publish` による `.so` の生成 → `g++` でビルドした呼び出し側からの実行（`total = 30`）→ `nm -D` での export シンボル確認（`km_accumulator_*` の 4 つ）」まで検証済みです。Windows 側の手順は記事本文と同じコマンドを記載しています。

## ポイント

- 境界面は .NET ではなく C ABI として設計する（`string` / `List<T>` / 例外を越境させない）
- ネイティブ側に見せるのは `intptr_t` の handle だけにして、状態本体は C# 側で持つ
- 戻り値はエラーコード、出力値はポインタ引数で返す
- `CallConvCdecl` を明示して呼び出し規約を固定する
- export メソッドは ABI の窓口に徹し、本体ロジックは別クラスに置く（テストもしやすくなる）
- RID ごとに publish し、呼び出し側と DLL の bitness を揃える
- Native AOT の共有ライブラリはアンロード前提では使わない（`FreeLibrary` / `dlclose` を呼ばない）

詳しい解説は[記事本文](https://comcomponent.com/blog/2026/03/12/003-csharp-native-aot-native-dll-from-c-cpp/)をご覧ください。
