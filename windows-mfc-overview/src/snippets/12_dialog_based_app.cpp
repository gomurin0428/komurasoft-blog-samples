// 記事 12 章「ダイアログベースのMFCアプリ」のコード断片。
//
// 設定画面や簡単な業務ツールでは CDialog / CDialogEx を継承した
// ダイアログ中心の構成がよく使われる。
// ダイアログは見た目だけでなく、リソース（IDD_...）、コントロールID（IDC_...）、
// メンバー変数、DoDataExchange、メッセージマップが組み合わさって動く。
//
// ※ 参照用スニペットです。ダイアログリソースがないため単体ではビルドできません。

#include <afxwin.h>
#include <afxdialogex.h>

class CSettingsDialog : public CDialogEx
{
public:
    CSettingsDialog(CWnd* pParent = nullptr);

#ifdef AFX_DESIGN_TIME
    enum { IDD = IDD_SETTINGS_DIALOG };
#endif

protected:
    virtual void DoDataExchange(CDataExchange* pDX);
    virtual BOOL OnInitDialog();

    afx_msg void OnBnClickedOk();
    DECLARE_MESSAGE_MAP()

private:
    CString m_name;
    int m_interval;
};
