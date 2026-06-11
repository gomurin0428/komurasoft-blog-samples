# Media FoundationでMP4に画像と文字を焼き込む方法 ── サンプルコード

ブログ記事「[Media FoundationでMP4に画像と文字を焼き込む方法](https://comcomponent.com/blog/2026/03/16/009-media-foundation-overlay-image-text-on-mp4-frames/)」のサンプルコードです。

Media Foundation で MP4 動画をデコードし、各フレームに画像と `HelloWorld` の文字を GDI+ で焼き込み、H.264 で再エンコードした新しい MP4 を出力するコンソールアプリです。
構成は記事のとおり **`Source Reader -> RGB32 -> GDI+ で描画 -> BGRA -> NV12 変換 -> Sink Writer`** で、1 つの `.cpp` に完結しています。出力は映像のみの MP4 です（音声 remux は記事 9 章で拡張方針として解説しています）。

## 構成

```
media-foundation-overlay-image-text-on-mp4-frames/
├── src/OverlayMp4.cpp   1 ファイル完結の実装（記事 6 章のコードそのまま）
└── CMakeLists.txt       MSVC 向けビルド構成
```

記事のサンプルは「Visual Studio の C++ コンソールアプリにそのまま貼れる 1 ファイル完結」が主旨のため、ファイル分割はせず `src/OverlayMp4.cpp` の 1 本にしています。

## 必要環境

- Windows 10 / 11（x64）
- Visual Studio 2022（「C++ によるデスクトップ開発」ワークロード）
- CMake を使う場合は CMake 3.20 以降

Media Foundation と GDI+ は Windows 専用 API のため、**Windows 以外ではビルド・実行できません**。

> **検証状態について**
> `src/OverlayMp4.cpp` は記事 6 章のコードを一字一句そのまま収録したものです（差分なしを確認済み）。このリポジトリの整備は Linux 環境で行ったため実行（実際の MP4 処理）は未確認ですが、MinGW-w64 クロスコンパイラ（x86_64-w64-mingw32-g++）で Windows 実行ファイル（PE32+）の生成（コンパイル・リンク）までは検証済みです。実行は Windows + Visual Studio 2022 でのビルドを前提としてください。

## ビルド方法

### 方法 1: Visual Studio 2022 のコンソールアプリに貼る（記事 5.1 節の手順）

1. Visual Studio で **Console App**（C++）プロジェクトを作る
2. `src/OverlayMp4.cpp` の内容をメインの `.cpp` に丸ごと貼る
3. その `.cpp` のプリコンパイル済みヘッダーを「**使用しない**」にする
4. `x64` でビルドする

### 方法 2: CMake でビルドする

「x64 Native Tools Command Prompt for VS 2022」などから:

```console
cmake -S . -B build -G "Visual Studio 17 2022" -A x64
cmake --build build --config Release
```

`build\Release\OverlayMp4.exe` が生成されます。

## 実行方法

```bat
OverlayMp4.exe input.mp4 overlay.png output.mp4
```

- `input.mp4` … 元動画（幅・高さが偶数であること。NV12 は 4:2:0 のため）
- `overlay.png` … 重ねたい画像（PNG / JPEG / BMP / GIF など GDI+ が読める形式）
- `output.mp4` … 出力先（映像のみの MP4）

焼き込む文字列はコード先頭の `kOverlayText`（`HelloWorld`）に固定で、位置・サイズも `kMarginRatio` などの定数で変えられます。

## 記事からの調整点

- コード本体は記事 6 章のものから**一切変更していません**。
- ビルド用に `CMakeLists.txt` を追加しています（リンクするライブラリはソース内の `#pragma comment(lib, ...)` と同じ `mfplat` / `mfreadwrite` / `mfuuid` / `mf` / `gdiplus` に、COM 初期化用の `ole32` を明示）。

## ポイント

- 描画しやすい形式（`RGB32 / BGRA`）と、H.264 エンコーダーが受けやすい形式（`NV12`）は別物。間に変換段を置く
- stride と上下向きの差は、top-down の BGRA バッファーへ正規化してから描画して吸収する（`CopySampleToTopDownBgra`）
- `ReadSample` は `HRESULT` だけでなく `flags`（`STREAMTICK` / `ENDOFSTREAM` など）と `sample == nullptr` を必ず見る
- timestamp / duration はできるだけ入力サンプルから引き継ぎ、取れないときだけ fps からの既定値にフォールバックする
- 画像と文字を載せるのは Media Foundation ではなく描画 API（ここでは GDI+）の仕事。性能が必要になったら `Direct2D / DirectWrite`、`Video Processor MFT`、`D3D11 surface` ベースへ段階的に進める

詳しい解説は[記事本文](https://comcomponent.com/blog/2026/03/16/009-media-foundation-overlay-image-text-on-mp4-frames/)をご覧ください。
