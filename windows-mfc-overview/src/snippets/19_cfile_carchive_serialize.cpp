// 記事 19 章「CFileとCArchive」のコード断片。
//
// CFile はファイル操作、CArchive は MFC のシリアライズ機構で使われる。
// CDocument 派生クラスでは Serialize をオーバーライドして、
// 読み込み（ar.IsStoring() == FALSE）と保存（TRUE）を同じ関数に書くことがある。
//
// 独自バイナリ形式を長く使っているアプリでは、Serialize が事実上の
// ファイル仕様になっていることもある。コード変更前に既存ファイルを読み込む
// テストデータを用意する。
//
// ※ 参照用スニペットです。CMyDocument / Item の定義がないため単体ではビルドできません。

#include <afxwin.h>

void OpenExample(LPCTSTR path)
{
    CFile file;
    if (file.Open(path, CFile::modeRead))
    {
        CArchive ar(&file, CArchive::load);
        // arから読み込む
    }
}

void CMyDocument::Serialize(CArchive& ar)
{
    if (ar.IsStoring())
    {
        ar << m_title;
        ar << static_cast<int>(m_items.size());
        for (const auto& item : m_items)
        {
            ar << item.Name;
            ar << item.Value;
        }
    }
    else
    {
        int count = 0;
        ar >> m_title;
        ar >> count;
        m_items.clear();
        for (int i = 0; i < count; ++i)
        {
            Item item;
            ar >> item.Name;
            ar >> item.Value;
            m_items.push_back(item);
        }
    }
}
