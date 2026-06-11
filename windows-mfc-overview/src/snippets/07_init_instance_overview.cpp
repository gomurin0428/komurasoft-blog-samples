// 記事 7 章「CWinAppは何をしているのか」のコード断片。
//
// 通常の Win32 アプリで自分で書く WinMain・ウィンドウクラス登録・メッセージループの
// 多くを MFC フレームワークが引き受けるため、開発者は主に InitInstance を
// オーバーライドしてアプリケーション固有の初期化を書く。
//
// ※ 参照用スニペットです。単体ではビルドできません。

#include <afxwin.h>

BOOL CMyApp::InitInstance()
{
    CWinApp::InitInstance();

    // 設定の読み込み
    // COM初期化
    // メインウィンドウ作成
    // ドキュメントテンプレート登録

    return TRUE;
}
