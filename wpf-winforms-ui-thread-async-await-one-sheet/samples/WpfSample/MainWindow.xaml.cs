using System.IO;
using System.Security.Cryptography;
using System.Windows;
using KomuraSoft.UiThreadAsyncAwait;

namespace WpfSample;

public partial class MainWindow : Window
{
    private readonly DocumentRepository _repository = new();

    public MainWindow()
    {
        InitializeComponent();
    }

    // ----------------------------------------------------------------
    // 4.1 UI イベントハンドラで plain await
    //
    // LoadButton_Click は UI スレッド上で始まり、
    // await File.ReadAllTextAsync(...) は plain await なので
    // その時点の UI コンテキストを捕まえる。
    // 読み込み完了後の続きは基本的に UI スレッドへ戻るため、
    // PreviewTextBox.Text = text; をそのまま書ける。
    // ----------------------------------------------------------------
    private async void LoadButton_Click(object sender, RoutedEventArgs e)
    {
        LoadButton.IsEnabled = false;
        StatusText.Text = "読み込み中...";

        try
        {
            string text = await File.ReadAllTextAsync(FilePathTextBox.Text);
            PreviewTextBox.Text = text;
            StatusText.Text = "完了";
        }
        catch (Exception ex)
        {
            StatusText.Text = ex.Message;
        }
        finally
        {
            LoadButton.IsEnabled = true;
        }
    }

    // ----------------------------------------------------------------
    // 4.2 重い CPU 計算だけ Task.Run
    //
    // I/O 待ちは非同期で流し、重いハッシュ計算だけ Task.Run で
    // ThreadPool に出す。await Task.Run(...) の続きは plain await
    // なので UI スレッドへ戻り、ResultText.Text = hash; をそのまま書ける。
    // Task.Run の中だけが別スレッド。
    // ----------------------------------------------------------------
    private async void HashButton_Click(object sender, RoutedEventArgs e)
    {
        HashButton.IsEnabled = false;
        ResultText.Text = "計算中...";

        try
        {
            byte[] data = await File.ReadAllBytesAsync(InputPathTextBox.Text);

            string hash = await Task.Run(() =>
            {
                using SHA256 sha256 = SHA256.Create();
                byte[] digest = sha256.ComputeHash(data);
                return Convert.ToHexString(digest);
            });

            ResultText.Text = hash;
        }
        catch (Exception ex)
        {
            ResultText.Text = ex.Message;
        }
        finally
        {
            HashButton.IsEnabled = true;
        }
    }

    // ----------------------------------------------------------------
    // 4.3 ライブラリ側は ConfigureAwait(false)、UI 側は plain await
    //
    // DocumentRepository（クラスライブラリ側）は ConfigureAwait(false) で
    // UI に戻らない。それを UI ハンドラが plain await すると、
    // 呼び出し元の続きは UI へ戻る、という分離ができる。
    // ----------------------------------------------------------------
    private async void OpenButton_Click(object sender, RoutedEventArgs e)
    {
        OpenButton.IsEnabled = false;
        StatusText.Text = "読み込み中...";

        try
        {
            string text = await _repository.LoadNormalizedTextAsync(
                PathTextBox.Text,
                CancellationToken.None);

            PreviewTextBox.Text = text;
            StatusText.Text = "完了";
        }
        catch (Exception ex)
        {
            StatusText.Text = ex.Message;
        }
        finally
        {
            OpenButton.IsEnabled = true;
        }
    }

    // ----------------------------------------------------------------
    // 5 章: ConfigureAwait(false) の続きから Dispatcher.InvokeAsync で
    // 明示的に UI へ戻す例。
    // ----------------------------------------------------------------
    private async void RefreshButton_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            await RefreshPreviewAsync(PathTextBox.Text, CancellationToken.None);
        }
        catch (Exception ex)
        {
            StatusText.Text = ex.Message;
        }
    }

    private async Task RefreshPreviewAsync(string path, CancellationToken cancellationToken)
    {
        string text = await File.ReadAllTextAsync(path, cancellationToken).ConfigureAwait(false);

        await Dispatcher.InvokeAsync(() =>
        {
            PreviewTextBox.Text = text;
            StatusText.Text = "完了";
        });
    }

    // ================================================================
    // ここから下は「やってはいけない例」（記事 4.3 章・4.4 章）。
    // 実行されないよう、どのボタンにも接続していません。
    // ================================================================

    // ----------------------------------------------------------------
    // やってはいけない例（記事 4.3 章）:
    // UI ハンドラ自身の await に ConfigureAwait(false) を付ける。
    //
    // この場合、この await の続きは UI に戻ることを強制しないため、
    // PreviewTextBox.Text = text; はクロススレッドアクセスになりえる。
    // ----------------------------------------------------------------
    private async void OpenButton_Click_ConfigureAwaitFalse_DoNotUse(object sender, RoutedEventArgs e)
    {
        string text = await _repository.LoadNormalizedTextAsync(
            PathTextBox.Text,
            CancellationToken.None).ConfigureAwait(false);

        PreviewTextBox.Text = text;
    }

    // ----------------------------------------------------------------
    // やってはいけない例（記事 4.4 章）:
    // UI スレッドで .Result により同期的に待つ。
    //
    // LoadTextAsync() の中の await は UI コンテキストを捕まえるため、
    // I/O 完了後の続きは UI スレッドへ戻りたい。しかし UI スレッドは
    // .Result で塞がっているので継続が走れず、.Result も終わらない。
    // つまりデッドロック（少なくとも UI フリーズ）になる。
    // ----------------------------------------------------------------
    private void LoadButton_Click_Deadlock_DoNotUse(object sender, RoutedEventArgs e)
    {
        string text = LoadTextAsync().Result;
        PreviewTextBox.Text = text;
    }

    private async Task<string> LoadTextAsync()
    {
        string text = await File.ReadAllTextAsync(FilePathTextBox.Text);
        return text.ToUpperInvariant();
    }
}
