using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using Inspection;

namespace InspectWpf;

public partial class MainWindow : Window
{
    public MainWindow() => InitializeComponent();

    private async void InspectButton_Click(object sender, RoutedEventArgs e)
    {
        const int width = 640, height = 480, stride = 640;
        byte[] frame = CaptureFrame(width, height, stride);  // カメラから受け取る画像の代わり

        FrameImage.Source = BitmapSource.Create(
            width, height, 96, 96, PixelFormats.Gray8, null, frame, stride);

        var parameters = new InspectParams { Threshold = 64, MaxDefectPixels = 50 };
        InspectButton.IsEnabled = false;
        bool fatal = false;
        try
        {
            // 検査は UI スレッドの外で行う。終わるまで frame を書き換えない
            var (status, result) = await Task.Run(() =>
            {
                var s = AdaInspector.Inspect(frame, width, height, stride, parameters, out var r);
                return (s, r);
            });

            // 99 は想定していない失敗。メモリの不正もありうるので、DLL をもう呼ばない
            fatal = status == InspectStatus.InternalError;
            ResultText.Text = status == InspectStatus.Ok
                ? $"{(result.Verdict == 0 ? "合格" : "不合格")}　欠陥 {result.DefectPixels} 画素"
                : fatal
                    ? "検査を止めました。ログを残してアプリを再起動してください"
                    : $"検査できませんでした（{status}）";
        }
        finally
        {
            InspectButton.IsEnabled = !fatal;
        }
    }

    private static byte[] CaptureFrame(int width, int height, int stride)
    {
        var frame = new byte[stride * height];
        frame.AsSpan().Fill(200);
        for (int y = 50; y < 60; y++)
        {
            frame.AsSpan(y * stride + 100, 10).Fill(20);
        }
        return frame;
    }
}
