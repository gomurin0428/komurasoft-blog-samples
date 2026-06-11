// 記事 13 章「DDXとDDV」のコード断片。
//
// DDX (Dialog Data Exchange) はダイアログ上のコントロールと C++ メンバー変数を
// 対応づける仕組み。DDV (Dialog Data Validation) は入力値を検証する仕組み。
//
//   UpdateData(TRUE)  : 画面の入力値 -> メンバー変数
//   UpdateData(FALSE) : メンバー変数 -> 画面
//
// 入力値がおかしいときは、DDX の定義、UpdateData の向きとタイミング、
// DDV による検証、コントロールIDとリソースの一致を確認する。
//
// ※ 参照用スニペットです。CSettingsDialog の定義（12 章）とリソースが前提です。

#include <afxwin.h>
#include <afxdialogex.h>

void CSettingsDialog::DoDataExchange(CDataExchange* pDX)
{
    CDialogEx::DoDataExchange(pDX);
    DDX_Text(pDX, IDC_EDIT_NAME, m_name);
    DDX_Text(pDX, IDC_EDIT_INTERVAL, m_interval);
    DDV_MinMaxInt(pDX, m_interval, 1, 3600);
}

// UpdateData(TRUE) で画面の入力値をメンバー変数へ反映してから使う
void CSettingsDialog::OnBnClickedOk()
{
    if (!UpdateData(TRUE))
    {
        return;
    }

    // ここでは m_name や m_interval に画面入力値が入っている
    SaveSettings(m_name, m_interval);

    CDialogEx::OnOK();
}

// UpdateData(FALSE) でメンバー変数の値を画面へ反映する
BOOL CSettingsDialog::OnInitDialog()
{
    CDialogEx::OnInitDialog();

    m_name = _T("default");
    m_interval = 60;
    UpdateData(FALSE);

    return TRUE;
}
