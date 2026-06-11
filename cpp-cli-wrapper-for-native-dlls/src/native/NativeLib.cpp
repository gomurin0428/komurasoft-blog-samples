// NativeLib.cpp
//
// 記事ではヘッダー（API イメージ）だけを示しているため、
// サンプルとしてビルド・動作確認できるようにダミー実装を補ったものです。
// 「C++ のクラス・std::wstring・std::vector・例外が境界に出てくる」
// という記事の前提を再現することが目的で、解析処理そのものに意味はありません。
#include "NativeLib.hpp"

#include <stdexcept>

namespace NativeLib
{
    Analyzer::Analyzer(const std::wstring& licensePath)
    {
        // 失敗時に C++ 例外が飛ぶ、という記事の前提を再現する。
        if (licensePath.empty())
        {
            throw std::invalid_argument("licensePath must not be empty.");
        }
    }

    AnalyzeResult Analyzer::Analyze(const std::wstring& imagePath, const AnalyzeOptions& options)
    {
        if (imagePath.empty())
        {
            throw std::invalid_argument("imagePath must not be empty.");
        }

        if (options.modelPath.empty())
        {
            throw std::runtime_error("modelPath must not be empty.");
        }

        // 本物の画像解析の代わりに、パスの文字コードから擬似スコアを作るダミー実装。
        AnalyzeResult result{};
        result.scores.reserve(imagePath.size());

        int maxScore = 0;

        for (wchar_t ch : imagePath)
        {
            int score = static_cast<int>(ch) % 100;
            result.scores.push_back(score);

            if (score > maxScore)
            {
                maxScore = score;
            }
        }

        result.ok = maxScore >= options.threshold;

        if (result.ok)
        {
            result.message = L"analysis completed (max score "
                + std::to_wstring(maxScore) + L")";
        }
        else
        {
            result.message = L"max score " + std::to_wstring(maxScore)
                + L" is below threshold " + std::to_wstring(options.threshold);
        }

        return result;
    }
}
