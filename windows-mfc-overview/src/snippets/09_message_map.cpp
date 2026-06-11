// 記事 9 章「メッセージマップとは何か」のコード断片。
//
// MFC では Win32 の WndProc の switch 文の代わりに、メッセージマップで
// Windows メッセージやコントロール通知をハンドラー関数に結びつける。
// このコードは「IDC_BUTTON_OK というボタンがクリックされたら
// CMyDialog::OnClickedButtonOk を呼び出す」という意味になる。
//
// 関数を検索しても直接呼び出しが見つからない場合は、
// BEGIN_MESSAGE_MAP / ON_... マクロを確認する。
//
// ※ 参照用スニペットです。CMyDialog の定義やリソースがないため単体ではビルドできません。

#include <afxwin.h>
#include <afxdialogex.h>

BEGIN_MESSAGE_MAP(CMyDialog, CDialogEx)
    ON_BN_CLICKED(IDC_BUTTON_OK, &CMyDialog::OnClickedButtonOk)
    ON_WM_CLOSE()
END_MESSAGE_MAP()

void CMyDialog::OnClickedButtonOk()
{
    AfxMessageBox(_T("Clicked"));
}
