// 記事 8 章「CWndはMFCの中心にあるクラス」のコード断片。
//
// CWnd オブジェクトと HWND は同じものではない。
// CWnd は HWND を扱いやすくする C++ ラッパーで、内部に HWND（m_hWnd）を持つ。
// CWnd* が存在していても、対応する HWND がすでに破棄されている可能性があるため、
// 操作前に ::IsWindow で有効性を確認する観点が重要になる。
//
// ※ 参照用スニペットです。単体ではビルドできません。

#include <afxwin.h>

void Example(CWnd* pWnd)
{
    // CWnd は内部に HWND を持つ（CWnd のメンバー関数内なら m_hWnd で参照できる）
    // HWND hWnd = m_hWnd;

    // あるいは GetSafeHwnd で取得する
    HWND hWnd = pWnd->GetSafeHwnd();
    UNREFERENCED_PARAMETER(hWnd);

    // ウィンドウが有効かどうかを確認してから操作する
    if (pWnd != nullptr && ::IsWindow(pWnd->GetSafeHwnd()))
    {
        pWnd->ShowWindow(SW_SHOW);
    }
}
