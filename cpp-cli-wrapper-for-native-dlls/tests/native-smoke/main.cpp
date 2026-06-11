// ネイティブ部分（NativeLib + NativeBridge）のスモークテスト。
// C++/CLI は Windows + MSVC でしかビルドできないため、
// プレーンな C++ で書かれたネイティブ部分だけを g++ / MSVC で検証します。
//
// Linux (g++) でのビルドと実行（1 行で実行する）:
//   g++ -std=c++17 -Wall -Wextra -I src/native tests/native-smoke/main.cpp
//       src/native/NativeLib.cpp src/native/NativeBridge.cpp -o native-smoke
//   ./native-smoke
#include "NativeBridge.hpp"
#include "NativeLib.hpp"

#include <iostream>
#include <stdexcept>
#include <string>

namespace
{
    int g_failed = 0;

    void Check(bool condition, const char* description)
    {
        if (condition)
        {
            std::cout << "  ok: " << description << "\n";
        }
        else
        {
            std::cout << "  NG: " << description << "\n";
            ++g_failed;
        }
    }
}

int main()
{
    // 1. C++ クラスを直接使う（C++/CLI ラッパーがやることと同じ呼び出し方）
    std::cout << "[NativeLib::Analyzer]\n";
    {
        NativeLib::Analyzer analyzer(L"C:\\license.dat");

        NativeLib::AnalyzeOptions options{};
        options.threshold = 80;
        options.modelPath = L"C:\\model.bin";

        NativeLib::AnalyzeResult result = analyzer.Analyze(L"C:\\input.png", options);

        Check(result.scores.size() == std::wstring(L"C:\\input.png").size(),
              "scores has one entry per character of imagePath");
        Check(!result.message.empty(), "message is not empty");

        bool threw = false;
        try
        {
            analyzer.Analyze(L"", options);
        }
        catch (const std::invalid_argument&)
        {
            threw = true;
        }
        Check(threw, "empty imagePath throws std::invalid_argument");
    }

    {
        bool threw = false;
        try
        {
            NativeLib::Analyzer analyzer(L"");
        }
        catch (const std::invalid_argument&)
        {
            threw = true;
        }
        Check(threw, "empty licensePath throws std::invalid_argument");
    }

    // 2. C API ブリッジ経由で使う（P/Invoke がやることと同じ呼び出し方）
    std::cout << "[NativeBridge C API]\n";
    {
        void* handle = Analyzer_Create(L"C:\\license.dat");
        Check(handle != nullptr, "Analyzer_Create returns a handle");

        AnalyzeOptionsNative options{};
        options.threshold = 80;
        options.modelPath = L"C:\\model.bin";

        AnalyzeResultNative result{};
        int error = Analyzer_Analyze(handle, L"C:\\input.png", &options, &result);

        Check(error == kAnalyzerResultOk, "Analyzer_Analyze returns kAnalyzerResultOk");
        Check(result.scoreCount > 0 && result.scoreCount <= kMaxScoreCount,
              "scoreCount is within the fixed-size buffer");
        Check(result.message[0] != L'\0', "message is not empty");

        // 例外はエラーコードに潰れる（詳細は失われる）
        error = Analyzer_Analyze(handle, L"", &options, &result);
        Check(error == kAnalyzerResultNativeError,
              "native exception is reduced to kAnalyzerResultNativeError");

        // null 引数はエラーコードになる
        error = Analyzer_Analyze(nullptr, L"C:\\input.png", &options, &result);
        Check(error == kAnalyzerResultInvalidArgument,
              "null handle returns kAnalyzerResultInvalidArgument");

        Analyzer_Destroy(handle);
    }

    {
        // 生成失敗は nullptr に潰れる（理由は分からない）
        void* handle = Analyzer_Create(L"");
        Check(handle == nullptr, "Analyzer_Create with empty licensePath returns nullptr");

        Analyzer_Destroy(nullptr); // null 安全であること（クラッシュしない）
        Check(true, "Analyzer_Destroy(nullptr) is safe");
    }

    if (g_failed != 0)
    {
        std::cout << g_failed << " check(s) failed.\n";
        return 1;
    }

    std::cout << "all checks passed.\n";
    return 0;
}
