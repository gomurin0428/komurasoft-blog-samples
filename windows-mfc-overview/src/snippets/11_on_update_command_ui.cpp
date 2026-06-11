// 記事 11 章「ON_UPDATE_COMMAND_UIとは何か」のコード断片。
//
// メニュー項目やツールバーボタンの有効/無効・チェック状態・表示テキストは
// ON_UPDATE_COMMAND_UI で更新する。
// 「ボタンがなぜかグレーアウトする」「メニューが押せない」といった挙動を
// 調べるときは、ON_UPDATE_COMMAND_UI を探すと原因が見つかることがある。
//
// ※ 参照用スニペットです。単体ではビルドできません。

#include <afxwin.h>

BEGIN_MESSAGE_MAP(CMainFrame, CFrameWnd)
    ON_COMMAND(ID_EDIT_DELETE, &CMainFrame::OnEditDelete)
    ON_UPDATE_COMMAND_UI(ID_EDIT_DELETE, &CMainFrame::OnUpdateEditDelete)
END_MESSAGE_MAP()

void CMainFrame::OnUpdateEditDelete(CCmdUI* pCmdUI)
{
    pCmdUI->Enable(CanDeleteCurrentItem());
}
