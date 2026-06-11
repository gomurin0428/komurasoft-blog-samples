# Media FoundationでMP4の指定時刻から静止画を切り出す方法 ── サンプルコード

ブログ記事「[Media FoundationでMP4の指定時刻から静止画を切り出す方法](https://comcomponent.com/blog/2026/03/15/000-media-foundation-extract-still-image-from-mp4-at-specific-time/)」のサンプルコードです。

`IMFSourceReader` を同期モードで使い、MP4 から**指定時刻に最も近いフレームを 1 枚取り出して PNG として保存する**、1 ファイル完結の C++ コンソールアプリです。`SetCurrentPosition` が exact seek ではないことを前提に、seek 後に timestamp を見ながら target の前後フレームを比較し、stride と上下向きを吸収して top-down BGRA に詰め直し、alpha を `0xFF` に固定してから WIC で PNG 保存します。外部ライブラリは使わず、Windows 標準 API（Media Foundation + WIC）だけで完結します。

## 構成

```
media-foundation-extract-still-image-from-mp4-at-specific-time/
├── src/ExtractFrameFromMp4.cpp   記事 10 章のフルコード（1 ファイル完結）
└── CMakeLists.txt                MSVC 向けビルド構成
```

コードは記事本文（10 章「`.cpp` にそのまま貼れるフルコード」）からそのまま転載しています。

## 必要環境

- Windows 10 / 11
- Visual Studio 2022（「C++ によるデスクトップ開発」ワークロード）
- CMake 3.20 以降（Visual Studio 同梱のもので可）

**注記**: コードは記事本文からの転載です（差分なしを確認済み）。Media Foundation / WIC は Windows 専用 API のため実行確認は Windows 上で行う必要がありますが、MinGW-w64 クロスコンパイラ（x86_64-w64-mingw32-g++）で Windows 実行ファイル（PE32+）の生成（コンパイル・リンク）までは検証済みです。Windows + Visual Studio でのビルド手順は以下のとおりです。

## ビルド方法

### CMake を使う場合

「Developer Command Prompt for VS 2022」などの開発者コマンドプロンプトで実行します。

```console
cmake -S . -B build -A x64
cmake --build build --config Release
```

実行ファイルは `build\Release\ExtractFrameFromMp4.exe` に生成されます。

### Visual Studio のプロジェクトに貼り付ける場合

記事に書いてあるとおり、Visual Studio の「C++ コンソールアプリ」プロジェクトに `src/ExtractFrameFromMp4.cpp` を 1 本の `.cpp` として追加するだけでも動きます。

- `#pragma comment(lib, ...)` を入れてあるので、追加のリンカー設定は基本的に不要です
- `pch.h` / `stdafx.h` があるテンプレートでも、コード先頭の `__has_include` で拾う形にしてあります
- プロジェクト側で独自のプリコンパイル済みヘッダーを強制している場合は、この `.cpp` だけ「プリコンパイル済みヘッダーを使用しない」にすれば通ります
- 実行構成は x64 推奨です

## 実行方法

```console
ExtractFrameFromMp4.exe <input.mp4> <seconds> <output.png>
```

例:

```console
ExtractFrameFromMp4.exe C:\work\input.mp4 12.345 C:\work\frame.png
```

成功すると、保存先パスと「要求した時刻」「実際に採用されたフレームの時刻」が表示されます。

## ポイント

- `SetCurrentPosition` は exact seek を保証しないので、seek 後に `ReadSample` を進めて target の前後フレームの timestamp を比較し、より近い 1 枚を採用する
- `ReadSample` は `S_OK` でも `pSample == nullptr` があり得るため、`HRESULT`・`flags`・`pSample` の 3 点セットで判定する（終端は `MF_SOURCE_READERF_ENDOFSTREAM`、ギャップは `MF_SOURCE_READERF_STREAMTICK`）
- `IMF2DBuffer::Lock2D` で実際の stride を取り、bottom-up（負の stride）も吸収して top-down の連続 BGRA に詰め直してから保存する
- `MFVideoFormat_RGB32` の 4 バイト目は alpha とは限らないので、PNG 保存前に `0xFF` で埋めて不透明にする
- `0 <= target < duration` を検証してから seek する

## 記事からの調整点

- なし（コードは記事 10 章のコードブロックを一字一句そのまま収録しています。CMakeLists.txt はこのリポジトリ用に追加したものです）

詳しい解説は[記事本文](https://comcomponent.com/blog/2026/03/15/000-media-foundation-extract-still-image-from-mp4-at-specific-time/)をご覧ください。
