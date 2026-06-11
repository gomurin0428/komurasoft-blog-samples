// 記事 26 章「Unicode、MBCS、TCHAR」のコード断片。
//
// TCHAR / LPCTSTR / _T() は、Unicode ビルドと MBCS ビルドの両方に対応するための書き方。
//
//   Unicodeビルド: TCHAR -> wchar_t, LPCTSTR -> const wchar_t*, _T("...") -> L"..."
//   MBCSビルド   : TCHAR -> char,    LPCTSTR -> const char*,    _T("...") -> "..."
//
// 現在は Unicode ビルドが一般的だが、古いアプリでは MBCS 前提の処理が残っていることがある。
// Unicode 化は単純な置換作業ではなく、ファイル互換性と外部連携まで確認する。
//
// ※ 参照用スニペットです。単体ではビルドできません。

#include <afxwin.h>

void Example(CWnd* pWnd)
{
    CString title = _T("設定");
    pWnd->SetWindowText(title);
}
