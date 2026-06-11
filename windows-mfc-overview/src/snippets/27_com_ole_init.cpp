// 記事 27 章「MFCとCOM/OLE/ActiveX」のコード断片。
//
// MFC アプリの起動処理（InitInstance）で OLE を初期化する典型例。
// COM/OLE を使っている場合、MFC の問題に見えて、実際には COM の初期化、
// スレッドモデル、参照カウント、登録情報、32bit/64bit の違いが原因に
// なっていることがある。x64 化のときは COM/OLE 依存も必ず確認する。
//
// ※ 参照用スニペットです。単体ではビルドできません。

#include <afxwin.h>
#include <afxdisp.h>

BOOL InitOleExample()
{
    if (!AfxOleInit())
    {
        AfxMessageBox(_T("OLE initialization failed"));
        return FALSE;
    }

    return TRUE;
}
