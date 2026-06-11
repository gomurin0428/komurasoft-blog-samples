// 記事 29 章「例外処理とエラー処理」のコード断片。
//
// MFC には TRY / CATCH / END_CATCH という独自の例外マクロがあり、
// 古いコードでは C++ 標準の try / catch と混在していることがある。
// MFC 例外と標準例外の混在、例外オブジェクトの寿命、戻り値エラーとの混在に注意。
//
// また、エラーが AfxMessageBox だけで終わっているとログが残らない。
// UI 表示とログ記録を分けると保守性が上がる。
//
// ※ 参照用スニペットです。単体ではビルドできません。

#include <afxwin.h>
#include <exception>

// --- 古い MFC の例外マクロ ---

void MfcExceptionMacroExample()
{
    TRY
    {
        // 処理
    }
    CATCH(CFileException, e)
    {
        e->ReportError();
    }
    END_CATCH
}

// --- 現代 C++ の try / catch（混在しているコードもある） ---

void StdExceptionExample()
{
    try
    {
        DoSomething();
    }
    catch (const std::exception& ex)
    {
        // ログ出力
    }
}

// --- 悪い例: 画面表示だけでログが残らない ---

void BadErrorReport()
{
    AfxMessageBox(_T("保存に失敗しました"));
}

// --- 改善例: UI 表示とログ記録を分ける ---

void BetterErrorReport(const CString& path)
{
    LogError(_T("Save failed"), path);
    AfxMessageBox(_T("保存に失敗しました。ログを確認してください。"));
}
