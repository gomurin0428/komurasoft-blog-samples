// 記事 31 章「テストしやすいMFCコードにする」のコード断片。
//
// CDialog や CView を直接テストしようとせず、まず非 UI ロジックを
// 純粋な C++ クラスへ切り出す。MFC 側は入力と出力だけを担当させる。
// この構造にすると、PriceCalculator や ParsePrices は通常の C++ テスト
// フレームワークでテストできる。
//
// ※ PriceCalculator は MFC 非依存の純粋な C++ クラスなので、この部分だけは
//    どの環境でもコンパイルできます。CPriceDialog 側は参照用です。

#include <vector>

// --- MFC に依存しない、テスト可能な純粋 C++ クラス ---

class PriceCalculator
{
public:
    int CalculateTotal(const std::vector<int>& prices) const
    {
        int total = 0;
        for (int price : prices)
        {
            total += price;
        }
        return total;
    }
};

// --- MFC 側は入力と出力だけを担当する（参照用） ---

#include <afxwin.h>

void CPriceDialog::OnBnClickedCalculate()
{
    if (!UpdateData(TRUE))
    {
        return;
    }

    std::vector<int> prices = ParsePrices(ToStdWString(m_input));
    int total = m_calculator.CalculateTotal(prices);

    m_result.Format(_T("%d"), total);
    UpdateData(FALSE);
}
