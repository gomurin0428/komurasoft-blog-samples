// 記事 18 章「CStringと文字列」のコード断片。
//
// CString は MFC/ATL 系コードでよく使われる可変長文字列クラス。
// プロジェクトの文字セット設定（Unicode / MBCS）に応じて実体が変わるため、
// 外部 API・ファイル形式・文字コードとセットで確認することが大切。
//
// ※ 参照用スニペットです。単体ではビルドできません。

#include <afxstr.h>
#include <string>

void Example()
{
    // CString の基本的な使い方
    CString name = _T("Komura");
    CString message;
    message.Format(_T("Hello, %s"), name.GetString());

    // CString から std::wstring への変換（Unicode ビルド前提）
    CString text = _T("日本語");
    std::wstring ws(text.GetString());
}
