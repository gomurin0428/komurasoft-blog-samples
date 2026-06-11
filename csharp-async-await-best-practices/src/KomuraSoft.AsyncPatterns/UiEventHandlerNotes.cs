namespace KomuraSoft.AsyncPatterns;

// 記事 4.2「async void はイベントハンドラだけ」の例は WinForms 依存
// （MessageBox / Label など）のため、この net8.0 クラスライブラリでは
// ビルド対象にせず、参考としてコメントで収録しています。
//
// async void はイベントハンドラ以外では避けるのが基本です。
// 理由: 呼び出し元が await できない / 完了を待てない / 例外処理が難しくなる / テストしづらい。
// イベントハンドラだけは void が必要なので、そこでだけ使い、
// 中で例外を握って UI 側へ返すところまで自分で書きます。
//
// private async void SaveButton_Click(object? sender, EventArgs e)
// {
//     try
//     {
//         await SaveAsync(_saveCancellation.Token);
//         _statusLabel.Text = "保存しました。";
//     }
//     catch (OperationCanceledException)
//     {
//         _statusLabel.Text = "キャンセルしました。";
//     }
//     catch (Exception ex)
//     {
//         MessageBox.Show(this, ex.Message, "保存エラー");
//     }
// }
