// 記事 14 章「Document/Viewアーキテクチャ」のコード断片。
//
// CDocument がデータ保持・ファイル読み書き・ビューへの更新通知を担当し、
// CView がデータの表示・ユーザー操作・描画を担当する。
// 同じデータを複数のビュー（表・グラフ・詳細・印刷など）で表示しやすくなる。
//
// ※ 参照用スニペットです。Item 型やドキュメントテンプレートの登録がないため
//    単体ではビルドできません。

#include <afxwin.h>
#include <vector>

class CMyDocument : public CDocument
{
public:
    std::vector<Item> m_items;

    virtual BOOL OnOpenDocument(LPCTSTR lpszPathName);
    virtual BOOL OnSaveDocument(LPCTSTR lpszPathName);
};

class CMyView : public CView
{
protected:
    virtual void OnDraw(CDC* pDC);

    CMyDocument* GetDocument() const;
};

// ビュー側ではドキュメントを取得して描画する
void CMyView::OnDraw(CDC* pDC)
{
    CMyDocument* pDoc = GetDocument();
    if (pDoc == nullptr)
    {
        return;
    }

    for (const auto& item : pDoc->m_items)
    {
        // pDCを使って描画する
    }
}
