# WindowsのMFCとは何か ── サンプルコード

ブログ記事「[WindowsのMFCとは何か ── 既存資産を保守するための基礎知識](https://comcomponent.com/blog/2026/06/11/000-windows-mfc-overview/)」のサンプルコードです。

MFC（Microsoft Foundation Classes）は、Win32 API を C++ のクラスとして扱いやすくする Windows ネイティブアプリケーション向けのフレームワークです。記事では、MFC の概要、アプリケーション構造、メッセージマップ、Document/View、DDX/DDV、リソース、ビルド、保守時の注意点を整理しています。

このフォルダは、記事に登場する C++ のコード断片を**章ごとに整理した参照用コード集**です。記事の各コードは「メッセージマップとは何か」「DDX/DDV の向き」「CWnd と HWND の寿命」など、それぞれ独立したトピックを説明するためのスニペットであり、1 つのアプリケーションを構成する部品ではありません。そのため、ビルド可能なプロジェクトとしては組み立てず、章番号付きのファイルに整理し、冒頭コメントで文脈を補足する形にしています。

## 構成

```
windows-mfc-overview/
└── src/snippets/                          章ごとに整理した参照用スニペット
    ├── 02_win32_wndproc_vs_mfc.cpp        Win32のWndProcとMFCのクラス表現（記事 2 章）
    ├── 06_cwinapp_basic_structure.cpp     CWinApp派生クラスとtheApp（記事 6 章）
    ├── 07_init_instance_overview.cpp      InitInstanceに書く初期化（記事 7 章）
    ├── 08_cwnd_and_hwnd.cpp               CWndとHWNDの関係・有効性確認（記事 8 章）
    ├── 09_message_map.cpp                 メッセージマップの基本（記事 9 章）
    ├── 10_command_routing.cpp             ON_COMMANDとコマンドルーティング（記事 10 章）
    ├── 11_on_update_command_ui.cpp        メニュー/ボタンの有効・無効制御（記事 11 章）
    ├── 12_dialog_based_app.cpp            CDialogEx派生クラスの典型形（記事 12 章）
    ├── 13_ddx_ddv.cpp                     DDX/DDVとUpdateDataの向き（記事 13 章）
    ├── 14_document_view.cpp               Document/Viewアーキテクチャ（記事 14 章）
    ├── 16_resource_ids.cpp                resource.hとリソースIDの結びつき（記事 16 章）
    ├── 17_class_wizard_markers.cpp        Class Wizardのコメントマーカー（記事 17 章）
    ├── 18_cstring_and_strings.cpp         CStringと文字列変換（記事 18 章）
    ├── 19_cfile_carchive_serialize.cpp    CFile/CArchiveとSerialize（記事 19 章）
    ├── 20_gdi_cdc_drawing.cpp             CDCによるGDI描画とペンの復元（記事 20 章）
    ├── 21_modal_modeless_dialog.cpp       モーダル/モードレスダイアログ（記事 21 章）
    ├── 22_object_and_handle_lifetime.cpp  C++オブジェクトとハンドルの寿命（記事 22 章）
    ├── 23_worker_thread_ui_update.cpp     ワーカースレッドからのUI更新（記事 23 章）
    ├── 24_mfc_dll_module_state.cpp        AFX_MANAGE_STATEとモジュール状態（記事 24 章）
    ├── 26_unicode_mbcs_tchar.cpp          TCHARと_T()マクロ（記事 26 章）
    ├── 27_com_ole_init.cpp                AfxOleInitによるOLE初期化（記事 27 章）
    ├── 28_high_dpi_drawing.cpp            高DPIで崩れる固定ピクセル描画（記事 28 章）
    ├── 29_exception_error_handling.cpp    MFC例外マクロとエラー処理（記事 29 章）
    ├── 30_mfc_and_modern_cpp.cpp          MFC層と非MFC層の分離（記事 30 章）
    ├── 31_testable_mfc_code.cpp           テスト可能なロジックの切り出し（記事 31 章）
    └── 32_precompiled_headers.cpp         プリコンパイル済みヘッダー（記事 32 章）
```

## このコード集の位置づけ

- **参照用コード集です。** 各ファイルは独立した解説スニペットで、1 つの MFC アプリとしてビルドする構成にはしていません。各スニペットは `CMyDialog` や `CSettingsDialog` のような説明用のクラスを前提にしており、ダイアログリソース（`.rc`）や `resource.h` を持たないため、単体ではコンパイルできません（例外として `31_testable_mfc_code.cpp` の `PriceCalculator` は MFC 非依存です）。
- 各ファイルの冒頭コメントに、対応する記事の章と、そのスニペットが説明している文脈を書いています。記事本文と対照しながら読むことを想定しています。

## MFC のコードを実際にビルドしたい場合

MFC は Windows + Visual Studio 専用です。次の環境が必要です。

1. [Visual Studio 2022](https://visualstudio.microsoft.com/)（Community で可）をインストールする
2. Visual Studio Installer で、ワークロード「**C++ によるデスクトップ開発**」を選択する
3. 個別コンポーネントで「**C++ MFC for latest v143 build tools (x86 & x64)**」を追加する（必要に応じて ATL も）
4. Visual Studio の新規プロジェクトで「MFC アプリ」テンプレート（ダイアログベースまたは SDI）を作成し、スニペットの該当部分を組み込んで試す

記事 5 章「Visual StudioでMFCを使う準備」と 33 章「CIでMFCをビルドする」も参考にしてください。

## 検証状態について（正直な注記）

このリポジトリの整備は Linux 環境で行っており、MFC（`afxwin.h` など）は Windows + Visual Studio の MFC コンポーネントでしか利用できないため、**このコード集はコンパイル検証していません**。コードは記事本文のコード断片を忠実に収録したものです。お手元の Visual Studio で試す際は、上記の手順で MFC コンポーネントを導入したうえで、MFC アプリプロジェクトに組み込んでください。

## ポイント

- MFC は Win32 API を隠すものではなく、C++ らしく包んだものと考える
- イベントと関数はメッセージマップ（`BEGIN_MESSAGE_MAP` / `ON_...`）で結びつく ── 関数の直接呼び出しを検索しても見つからないときはここを見る
- `CWnd*` が非 null でも `HWND` が有効とは限らない ── `::IsWindow(GetSafeHwnd())` で確認する
- `UpdateData(TRUE)` は画面→変数、`UpdateData(FALSE)` は変数→画面
- ワーカースレッドから UI を直接触らず、`PostMessage` で UI スレッドへ通知する
- MFC DLL の入口では `AFX_MANAGE_STATE(AfxGetStaticModuleState())` を忘れない
- 保守で最も効く改善は、UI クラスからテスト可能なロジックを少しずつ切り出すこと

詳しい解説は[記事本文](https://comcomponent.com/blog/2026/06/11/000-windows-mfc-overview/)をご覧ください。
