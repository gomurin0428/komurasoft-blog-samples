// 記事 22 章「C++オブジェクトとWindowsハンドルの寿命」のコード断片。
//
// CWnd は C++ オブジェクトだが、実際のウィンドウは Windows が HWND として管理する。
// この 2 つは常に同時に作られて同時に消えるわけではない。
// GetDlgItem で得た CWnd* を長期間保持すると、ウィンドウ破棄後に参照する危険がある。
// MFC ではポインタが非 null でも安全とは限らない。
//
// ※ 参照用スニペットです。単体ではビルドできません。

#include <afxwin.h>

void Example()
{
    // 注意が必要なコード:
    // GetDlgItem で得たポインタをメンバーに保存して後で使うのは危険
    CWnd* pWnd = GetDlgItem(IDC_SOME_CONTROL);
    // pWndをメンバーに保存して後で使う
}

// 必要なら毎回 GetDlgItem するか、コントロール用のメンバー変数を DDX で管理する
// （DoDataExchange の中に書く）
//
// DDX_Control(pDX, IDC_LIST_ITEMS, m_listItems);

void SafeAccessExample()
{
    // ポインタが非 null でも、HWND が有効かどうかを確認してから操作する
    if (m_pDialog != nullptr && ::IsWindow(m_pDialog->GetSafeHwnd()))
    {
        m_pDialog->SetWindowText(_T("Running"));
    }
}
