// 記事 23 章「スレッドとUI更新」のコード断片。
//
// Windows の UI は、基本的に作成された UI スレッドで操作する必要がある。
// ワーカースレッドから直接 UI コントロールを操作すると、不安定な動作や
// クラッシュの原因になる。一般的には PostMessage で UI スレッドへ通知し、
// UI 側はメッセージマップ（ON_MESSAGE）で受ける。
//
// ※ 参照用スニペットです。CMyDialog の定義がないため単体ではビルドできません。

#include <afxwin.h>
#include <afxdialogex.h>

// --- 避けたい例: ワーカースレッドから直接 UI を触る ---

UINT WorkerThreadProc(LPVOID pParam)
{
    CMyDialog* pDlg = static_cast<CMyDialog*>(pParam);

    // ワーカースレッドから直接UIを触るのは避ける
    pDlg->SetDlgItemText(IDC_STATUS, _T("Done"));

    return 0;
}

// --- 推奨: PostMessage で UI スレッドへ通知する ---

constexpr UINT WM_APP_WORK_DONE = WM_APP + 1;

UINT WorkerThreadProc(LPVOID pParam)
{
    HWND hWnd = static_cast<HWND>(pParam);

    // 重い処理

    ::PostMessage(hWnd, WM_APP_WORK_DONE, 0, 0);
    return 0;
}

// --- UI 側はメッセージマップで受ける ---

BEGIN_MESSAGE_MAP(CMyDialog, CDialogEx)
    ON_MESSAGE(WM_APP_WORK_DONE, &CMyDialog::OnWorkDone)
END_MESSAGE_MAP()

LRESULT CMyDialog::OnWorkDone(WPARAM, LPARAM)
{
    SetDlgItemText(IDC_STATUS, _T("Done"));
    return 0;
}
