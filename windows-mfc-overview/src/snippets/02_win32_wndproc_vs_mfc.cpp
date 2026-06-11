// 記事 2 章「MFCとは何か」のコード断片。
//
// 前半: Win32 API を直接使う場合の典型的なウィンドウプロシージャ。
//       C の関数・ハンドル・メッセージ・コールバックを中心に組み立てる。
// 後半: 同じ「ウィンドウ」を MFC のクラス（CFrameWnd 派生）として表現した例。
//
// ※ 参照用スニペットです。単体ではビルドできません。

#include <afxwin.h>

// --- Win32 API を直接使う場合 ---

LRESULT CALLBACK WndProc(HWND hWnd, UINT message, WPARAM wParam, LPARAM lParam)
{
    switch (message)
    {
    case WM_PAINT:
        // 描画処理
        break;
    case WM_DESTROY:
        PostQuitMessage(0);
        break;
    default:
        return DefWindowProc(hWnd, message, wParam, lParam);
    }
    return 0;
}

// --- MFC でフレームウィンドウをクラスとして表現する場合 ---

class CMainFrame : public CFrameWnd
{
public:
    CMainFrame();

protected:
    afx_msg int OnCreate(LPCREATESTRUCT lpCreateStruct);
    DECLARE_MESSAGE_MAP()
};
