// 記事 21 章「モーダルダイアログとモードレスダイアログ」のコード断片。
//
// モーダル: DoModal で表示し、閉じるまで呼び出し元は待つ。
// モードレス: Create で表示し、呼び出し元の処理はすぐ戻る。
//
// モードレスダイアログでは寿命管理が重要。
// （いつ delete するか、親が先に破棄されないか、PostNcDestroy を使うか、
//   二重生成されないか、閉じた後のポインタが残っていないか）
//
// ※ 参照用スニペットです。各ダイアログクラスとリソースがないため単体ではビルドできません。

#include <afxwin.h>

void ShowModalExample(CWnd* pThis)
{
    // --- モーダルダイアログ ---
    CSettingsDialog dlg(pThis);
    if (dlg.DoModal() == IDOK)
    {
        // OK時の処理
    }
}

void ShowModelessExample()
{
    // --- モードレスダイアログ ---
    m_pToolDialog = new CToolDialog(this);
    m_pToolDialog->Create(IDD_TOOL_DIALOG, this);
    m_pToolDialog->ShowWindow(SW_SHOW);
}
