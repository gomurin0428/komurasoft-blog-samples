using KomuraSoft.UiThreadAsyncAwait;

namespace WinFormsSample;

/// <summary>
/// WinForms 版のサンプル。
/// Click ハンドラの中で plain await している限り、続きは基本的に UI 側へ戻る
/// という見方は WPF と同じ（記事 4.1 章）。
/// 明示的に UI へ戻す API が WPF の Dispatcher ではなく
/// Control.BeginInvoke / Invoke / InvokeAsync(.NET 9+) になる点が違う（記事 5 章）。
/// </summary>
public sealed class MainForm : Form
{
    private readonly TextBox filePathTextBox;
    private readonly Button loadButton;
    private readonly Button refreshButton;
    private readonly Label statusLabel;
    private readonly TextBox previewTextBox;

    private readonly DocumentRepository _repository = new();

    public MainForm()
    {
        Text = "WinForms UIスレッドと async/await のサンプル";
        ClientSize = new Size(600, 420);

        filePathTextBox = new TextBox
        {
            Location = new Point(12, 12),
            Width = 460,
        };
        loadButton = new Button
        {
            Location = new Point(480, 10),
            Text = "読み込み",
        };
        refreshButton = new Button
        {
            Location = new Point(480, 40),
            Text = "更新",
        };
        statusLabel = new Label
        {
            Location = new Point(12, 44),
            Width = 460,
        };
        previewTextBox = new TextBox
        {
            Location = new Point(12, 72),
            Size = new Size(576, 336),
            Multiline = true,
            ReadOnly = true,
            ScrollBars = ScrollBars.Vertical,
        };

        loadButton.Click += loadButton_Click;
        refreshButton.Click += refreshButton_Click;

        Controls.Add(filePathTextBox);
        Controls.Add(loadButton);
        Controls.Add(refreshButton);
        Controls.Add(statusLabel);
        Controls.Add(previewTextBox);
    }

    // ----------------------------------------------------------------
    // 4.1 UI イベントハンドラで plain await（WinForms 版）
    //
    // Click ハンドラの中で plain await している限り、
    // 続きは基本的に UI 側（WindowsFormsSynchronizationContext）へ戻る。
    // そのためコントロールをそのまま触れる。
    // ----------------------------------------------------------------
    private async void loadButton_Click(object? sender, EventArgs e)
    {
        loadButton.Enabled = false;
        statusLabel.Text = "読み込み中...";

        try
        {
            string text = await _repository.LoadNormalizedTextAsync(
                filePathTextBox.Text,
                CancellationToken.None);

            previewTextBox.Text = text;
            statusLabel.Text = "完了";
        }
        catch (Exception ex)
        {
            statusLabel.Text = ex.Message;
        }
        finally
        {
            loadButton.Enabled = true;
        }
    }

    private async void refreshButton_Click(object? sender, EventArgs e)
    {
        try
        {
            await RefreshPreviewAsync(filePathTextBox.Text, CancellationToken.None);
        }
        catch (Exception ex)
        {
            statusLabel.Text = ex.Message;
        }
    }

    // ----------------------------------------------------------------
    // 5 章: ConfigureAwait(false) の続きから明示的に UI へ戻す例。
    //
    // .NET 9 以降なら Control.InvokeAsync が async フローと相性がよい:
    //
    //   private async Task RefreshPreviewAsync(string path, CancellationToken cancellationToken)
    //   {
    //       string text = await File.ReadAllTextAsync(path, cancellationToken).ConfigureAwait(false);
    //
    //       await previewTextBox.InvokeAsync(() =>
    //       {
    //           previewTextBox.Text = text;
    //           statusLabel.Text = "完了";
    //       });
    //   }
    //
    // このサンプルは .NET 8 なので、旧来パターンの BeginInvoke を使う。
    // BeginInvoke は UI スレッドへ投稿してすぐ返る（呼び出し側をブロックしない）。
    // ----------------------------------------------------------------
    private async Task RefreshPreviewAsync(string path, CancellationToken cancellationToken)
    {
        string text = await File.ReadAllTextAsync(path, cancellationToken).ConfigureAwait(false);

        previewTextBox.BeginInvoke(() =>
        {
            previewTextBox.Text = text;
            statusLabel.Text = "完了";
        });
    }
}
