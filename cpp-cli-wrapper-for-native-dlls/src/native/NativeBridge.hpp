// NativeBridge.hpp
//
// 記事 6.2 の「C API に落としたブリッジのイメージ」を、実際にビルドできる形に
// 補ったヘッダーです。extern "C" の関数宣言は記事のままです。
// 記事では省略されている AnalyzeOptionsNative / AnalyzeResultNative の定義と、
// Linux の g++ で構文・動作確認するための移植性シムをサンプル側で補っています。
#pragma once

#include <cwchar>

// 移植性シム: __declspec は MSVC 拡張なので、Windows 以外では無効化する。
// （この環境では C++/CLI をビルドできないため、ネイティブ部分だけ g++ で検証できるようにする）
#if !defined(_WIN32)
#define __declspec(x)
#endif

// C API 用の構造体。
// C# 側（PInvokeConsumer/NativeInterop.cs）の AnalyzeOptionsNative /
// AnalyzeResultNative とメモリレイアウトを一致させる必要がある。
struct AnalyzeOptionsNative
{
    int threshold;
    const wchar_t* modelPath;
};

constexpr int kMaxMessageLength = 256;
constexpr int kMaxScoreCount = 16;

struct AnalyzeResultNative
{
    int ok; // 0 = false, 1 = true
    wchar_t message[kMaxMessageLength]; // 固定長バッファに切り詰める（C API のつらさの例）
    int scoreCount;
    int scores[kMaxScoreCount]; // std::vector をそのまま返せないので固定長にする
};

// Analyzer_Analyze の戻り値（エラーコード）。
// C 境界では C++ 例外を外に出せないため、戻り値に潰すことになる。
constexpr int kAnalyzerResultOk = 0;
constexpr int kAnalyzerResultInvalidArgument = 1;
constexpr int kAnalyzerResultNativeError = 2;

// C API に落としたブリッジのイメージ
extern "C"
{
    __declspec(dllexport) void* Analyzer_Create(const wchar_t* licensePath);
    __declspec(dllexport) void  Analyzer_Destroy(void* handle);

    __declspec(dllexport) int Analyzer_Analyze(
        void* handle,
        const wchar_t* imagePath,
        const AnalyzeOptionsNative* options,
        AnalyzeResultNative* result);
}
