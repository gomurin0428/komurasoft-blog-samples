// 記事 30 章「MFCと現代C++をどう共存させるか」のコード断片。
//
// UI 層は MFC の作法を尊重しつつ、ドメインロジックや計算処理は現代 C++ で整理する。
// 悪い形は、すべての処理がダイアログクラスに詰め込まれている状態。
// 改善するなら、MFC クラスからロジックを外（サービスクラス）へ出す。
// そうすると、m_service.Execute は MFC なしでテストできる。
//
// ※ 参照用スニペットです。CMainDialog / ExecuteRequest / ExecuteResult /
//    変換関数の定義がないため単体ではビルドできません。

#include <afxwin.h>

// --- 悪い形: すべての処理がダイアログクラスに詰め込まれている ---

void CMainDialog::OnBnClickedExecute()
{
    // 入力取得
    // ファイル読み込み
    // 通信
    // 計算
    // DB更新
    // 画面更新
    // ログ出力
    // 例外処理
}

// --- 改善例: MFC クラスからロジックを外へ出す ---

void CMainDialog::OnBnClickedExecute()
{
    if (!UpdateData(TRUE))
    {
        return;
    }

    ExecuteRequest request;
    request.Name = ToStdWString(m_name);
    request.Interval = m_interval;

    ExecuteResult result = m_service.Execute(request);

    m_status = ToCString(result.Message);
    UpdateData(FALSE);
}
