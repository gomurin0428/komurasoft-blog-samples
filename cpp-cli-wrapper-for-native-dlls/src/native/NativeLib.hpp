// NativeLib.hpp
#pragma once
#include <string>
#include <vector>

namespace NativeLib
{
    struct AnalyzeOptions
    {
        int threshold;
        std::wstring modelPath;
    };

    struct AnalyzeResult
    {
        bool ok;
        std::wstring message;
        std::vector<int> scores;
    };

    class Analyzer
    {
    public:
        explicit Analyzer(const std::wstring& licensePath);
        AnalyzeResult Analyze(const std::wstring& imagePath, const AnalyzeOptions& options);
    };
}
