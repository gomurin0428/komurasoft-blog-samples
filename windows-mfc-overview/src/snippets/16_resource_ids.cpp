// 記事 16 章「リソースファイルを理解する」のコード断片。
//
// MFC アプリでは .rc（リソースファイル）と resource.h（リソースID定義）が非常に重要。
// C++ コードはこれらの ID を使ってリソースと結びつく。
// 保守時にありがちな問題は、resource.h の ID 変更・マージによる衝突・
// ダイアログ上のコントロールIDと DDX の ID の不一致など。
//
// ※ 参照用スニペットです。単体ではビルドできません。

// --- resource.h に定義されるリソースIDの例 ---

#define IDD_SETTINGS_DIALOG  101
#define IDC_EDIT_NAME        1001
#define IDC_EDIT_INTERVAL    1002
#define ID_FILE_OPEN         32771

// --- これらの ID を使ってリソースと C++ コードを結びつける例 ---
// （DoDataExchange の中とメッセージマップの中で使う）

// DDX_Text(pDX, IDC_EDIT_NAME, m_name);
// ON_COMMAND(ID_FILE_OPEN, &CMainFrame::OnFileOpen)
