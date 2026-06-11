// 記事 6 章「MFCアプリケーションの基本構造」のコード断片。
//
// シンプルな MFC アプリの CWinApp 派生クラスの例。
// MFC アプリケーションでは、CWinApp から派生したグローバルオブジェクト
// （ここでは theApp）が通常 1 つ存在し、InitInstance が起動時の入口になる。
//
// ※ 参照用スニペットです。CMainFrame の定義やリソースがないため単体ではビルドできません。

#include <afxwin.h>

class CMyApp : public CWinApp
{
public:
    virtual BOOL InitInstance();
};

CMyApp theApp;

BOOL CMyApp::InitInstance()
{
    CWinApp::InitInstance();

    CMainFrame* pFrame = new CMainFrame;
    m_pMainWnd = pFrame;

    pFrame->Create(nullptr, _T("My MFC Application"));
    pFrame->ShowWindow(SW_SHOW);
    pFrame->UpdateWindow();

    return TRUE;
}
