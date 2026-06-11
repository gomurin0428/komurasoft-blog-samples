# Media FoundationでYUVをRGBに変換する方法 ── サンプルコード

ブログ記事「[Media FoundationでYUVをRGBに変換する方法](https://comcomponent.com/blog/2026/03/15/002-media-foundation-yuv-to-rgb-conversion-patterns/)」のサンプルコードです。

Media Foundation の decoder から出てくるフレームは `NV12` や `YUY2` のような YUV 系フォーマットが普通です。このサンプルでは、記事で整理している 2 つのパターンを提供します。

- **パターンA**: `MF_SOURCE_READER_ENABLE_VIDEO_PROCESSING` を有効にして、`IMFSourceReader` に `RGB32` まで自動で変換させる
- **パターンB**: `NV12` / `YUY2` を受け取り、`MF_MT_YUV_MATRIX` / `MF_MT_VIDEO_NOMINAL_RANGE` / stride を確認した上で、自分で BGRA32 へ変換する

## 構成

```
media-foundation-yuv-to-rgb-conversion-patterns/
├── src/
│   ├── PatternA_SourceReaderAutoRgb32.cpp  パターンA: Source Reader の自動変換（記事 4.3 章）
│   ├── PatternB_ManualYuvToBgra.cpp        パターンB: NV12/YUY2 の自前変換（記事 5.3〜5.9 章）
│   └── YuvPixelToBgra.h                    1 pixel の YUV→BGRA 変換式（記事 5.6 章、OS 非依存）
├── tests/
│   ├── MfStub.h                            Linux テスト用の最小限の Windows 型定義
│   └── TestYuvPixelToBgra.cpp              ピクセル変換の assert ベースのテスト
└── CMakeLists.txt
```

記事 5.6 章の 1 pixel 変換（`ClampToByte` / `ConvertLimitedYuvPixelToBgra`）は OS 非依存のピクセル演算なので、`src/YuvPixelToBgra.h` に分離しています。内容は記事のコードのままで、Windows では `<windows.h>` などの型定義を、Linux のテストでは `tests/MfStub.h` の互換定義を、それぞれ先に include して使います。

また、記事 5.9 章末尾の呼び出し側のコード片は、コンパイルできるように `ReadAndConvertOneFrameExample` 関数で包んでいます（中身は記事のままです）。

## 必要環境

- Media Foundation 部分（`src/` の 2 つの .cpp）: Windows 10 以降 + Visual Studio 2019/2022（MSVC）+ CMake 3.20 以降
- ピクセル変換のテスト（`tests/`）: g++ または MSVC（OS を問いません）

## ビルド方法

Windows（MSVC）:

```console
cmake -S . -B build
cmake --build build --config Release
ctest --test-dir build -C Release
```

Linux（テストのみ）:

```console
g++ -std=c++17 -Wall -Wextra -o test_yuv_pixel tests/TestYuvPixelToBgra.cpp
./test_yuv_pixel
```

## 検証状態

このサンプルは Linux 環境で作成しているため、検証できた範囲とできていない範囲を明記します。

- **検証済み**: `src/YuvPixelToBgra.h`（記事 5.6 章の 1 pixel 変換）は、g++ 13 で `-Wall -Wextra -Werror` でコンパイルし、テストがすべて成功することを確認済みです。limited range の黒・白・グレー、BT.601 / BT.709 のカラーバー代表値（赤・緑・青）、clip 動作、未対応 matrix の拒否（`MF_E_INVALIDMEDIATYPE`）を検証しています
- **コンパイルのみ検証**: `src/PatternA_SourceReaderAutoRgb32.cpp` と `src/PatternB_ManualYuvToBgra.cpp` は Media Foundation（Windows 専用 API）に依存するため実行確認は Windows 上で行う必要がありますが、MinGW-w64 クロスコンパイラ（x86_64-w64-mingw32-g++ -std=c++17）でのコンパイルは成功を確認済みです。コードは記事本文の掲載コードを忠実に収録したものです

## ポイント

- 楽をしたいなら Source Reader に `RGB32` を出させる。大量処理や色の制御まで欲しいなら YUV のまま受けて自分で変換する
- 自動変換は software 処理で、リアルタイム再生向けには最適化されていない
- 自前変換では `MF_MT_YUV_MATRIX` と `MF_MT_VIDEO_NOMINAL_RANGE` を必ず見て、対応していない組み合わせはエラーにする
- stride を `width * bytesPerPixel` だと決め打ちしない。`IMF2DBuffer::Lock2D` が返す actual stride を優先する
- `NV12` は 2x2 block で、`YUY2` は横 2 pixel で U/V を共有する
- `RGB32` の 4 byte 目は Alpha or Don't Care なので、`BGRA` として保存する前に `0xFF` を入れる

詳しい解説は[記事本文](https://comcomponent.com/blog/2026/03/15/002-media-foundation-yuv-to-rgb-conversion-patterns/)をご覧ください。
