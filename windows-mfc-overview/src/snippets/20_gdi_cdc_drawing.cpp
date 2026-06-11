// 記事 20 章「GDI描画とCDC」のコード断片。
//
// CDC は Windows の Device Context を扱う MFC クラス。CView::OnDraw に CDC* が渡される。
// ペンやブラシを SelectObject したら、必ず元のオブジェクトに戻すこと。
// 戻し忘れ・GDI オブジェクトの大量生成・OnPaint と OnDraw の混同・
// ダブルバッファリング不足によるちらつき・高 DPI での固定ピクセル描画の崩れに注意。
//
// ※ 参照用スニペットです。CMyView の定義がないため単体ではビルドできません。

#include <afxwin.h>

// --- 基本的な描画 ---

void CMyView::OnDraw(CDC* pDC)
{
    pDC->TextOut(10, 10, _T("Hello MFC"));
    pDC->Rectangle(10, 40, 200, 120);
}

// --- ペンの選択と復元 ---

void CMyView::OnDraw(CDC* pDC)
{
    CPen pen(PS_SOLID, 1, RGB(0, 0, 0));
    CPen* pOldPen = pDC->SelectObject(&pen);

    pDC->MoveTo(10, 10);
    pDC->LineTo(100, 100);

    pDC->SelectObject(pOldPen);
}
