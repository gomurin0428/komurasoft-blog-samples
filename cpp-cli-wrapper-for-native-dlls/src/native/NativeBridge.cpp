// NativeBridge.cpp
//
// 記事 6.2 のブリッジ関数の実装例です。
// 「P/Invoke を選んだつもりが、実質的には C 互換 API の設計を始めている」
// という記事の指摘どおり、ここでは
//   - 例外を戻り値（エラーコード）に潰す
//   - std::wstring / std::vector を固定長バッファに切り詰める
//   - 所有権を Create / Destroy のペアで表現する
// という変換を全部手書きすることになります。
#include "NativeBridge.hpp"
#include "NativeLib.hpp"

namespace
{
    void CopyMessage(const std::wstring& source, wchar_t (&destination)[kMaxMessageLength])
    {
        size_t length = source.size();

        if (length >= kMaxMessageLength)
        {
            length = kMaxMessageLength - 1; // 黙って切り詰める（情報が落ちる）
        }

        for (size_t i = 0; i < length; ++i)
        {
            destination[i] = source[i];
        }

        destination[length] = L'\0';
    }
}

extern "C"
{
    void* Analyzer_Create(const wchar_t* licensePath)
    {
        if (licensePath == nullptr)
        {
            return nullptr;
        }

        try
        {
            return new NativeLib::Analyzer(licensePath);
        }
        catch (...)
        {
            // C 境界を例外が越えてはいけないので、失敗の詳細は nullptr に潰れる。
            return nullptr;
        }
    }

    void Analyzer_Destroy(void* handle)
    {
        delete static_cast<NativeLib::Analyzer*>(handle);
    }

    int Analyzer_Analyze(
        void* handle,
        const wchar_t* imagePath,
        const AnalyzeOptionsNative* options,
        AnalyzeResultNative* result)
    {
        if (handle == nullptr || imagePath == nullptr || options == nullptr || result == nullptr)
        {
            return kAnalyzerResultInvalidArgument;
        }

        try
        {
            NativeLib::AnalyzeOptions nativeOptions{};
            nativeOptions.threshold = options->threshold;
            nativeOptions.modelPath = options->modelPath != nullptr ? options->modelPath : L"";

            NativeLib::AnalyzeResult nativeResult =
                static_cast<NativeLib::Analyzer*>(handle)->Analyze(imagePath, nativeOptions);

            result->ok = nativeResult.ok ? 1 : 0;
            CopyMessage(nativeResult.message, result->message);

            // 可変長の scores を固定長バッファに切り詰めて返す。
            int count = static_cast<int>(nativeResult.scores.size());

            if (count > kMaxScoreCount)
            {
                count = kMaxScoreCount;
            }

            result->scoreCount = count;

            for (int i = 0; i < count; ++i)
            {
                result->scores[i] = nativeResult.scores[i];
            }

            return kAnalyzerResultOk;
        }
        catch (...)
        {
            // 例外の型もメッセージも、ここで失われる。
            return kAnalyzerResultNativeError;
        }
    }
}
