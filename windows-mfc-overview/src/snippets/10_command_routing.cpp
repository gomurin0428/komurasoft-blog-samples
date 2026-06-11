// 記事 10 章「コマンドルーティング」のコード断片。
//
// メニューやツールバーの操作は ON_COMMAND でコマンドとして扱われる。
// MFC にはコマンドを「アクティブビュー → ドキュメント → フレームウィンドウ →
// アプリケーション」の順で配送する仕組みがあるため、
// 「メニューを押したらどの関数が呼ばれるのか」を文字列検索だけで追いにくいことがある。
//
// ※ 参照用スニペットです。CMainFrame の定義やリソースがないため単体ではビルドできません。

#include <afxwin.h>

BEGIN_MESSAGE_MAP(CMainFrame, CFrameWnd)
    ON_COMMAND(ID_FILE_OPEN, &CMainFrame::OnFileOpen)
END_MESSAGE_MAP()

void CMainFrame::OnFileOpen()
{
    // ファイルを開く処理
}
