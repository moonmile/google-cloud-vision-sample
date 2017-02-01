using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using Windows.Foundation;
using Windows.Graphics.Imaging;
using Windows.Media.Ocr;
using Windows.Storage.Streams;

namespace MsOcrWPF
{
    /// <summary>
    /// MainWindow.xaml の相互作用ロジック
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
        }

        string screenFile = "screen.png";

        private async void clickDetect(object sender, RoutedEventArgs e)
        {
            var bitmap = await LoadImage(screenFile);
            var result = await detect(bitmap);
            text1.Text = result.Text;

            // 認識した個所をマークする
            var bmp = Bitmap.FromFile(screenFile) as Bitmap;
            var g = Graphics.FromImage(bmp);
            var br = new SolidBrush(System.Drawing.Color.FromArgb(0x80, System.Drawing.Color.Blue));
            var text = "";
            foreach (var line in result.Lines)
            {
                text += line.Text + " ";
                foreach (var it in line.Words)
                {
                    var rc = new System.Drawing.Rectangle(
                        (int)it.BoundingRect.X, (int)it.BoundingRect.Y,
                        (int)it.BoundingRect.Width, (int)it.BoundingRect.Height);
                    g.FillRectangle(br, rc);
                    g.DrawRectangle(Pens.Red, rc);
                    text += it.Text + " ";
                }
            }
            image.Source = bmp.ToImageSource();
        }

        /// <summary>
        /// Microsoft OCR の呼び出し
        /// </summary>
        /// <param name="bitmap"></param>
        /// <returns></returns>
        public async Task<OcrResult> detect(SoftwareBitmap bitmap)
        {
            var ocrEngine = OcrEngine.TryCreateFromUserProfileLanguages();
            var ocrResult = await ocrEngine.RecognizeAsync(bitmap);
            return ocrResult;
        }

        /// <summary>
        /// ファイルパスを指定して SoftwareBitmap を取得
        /// </summary>
        /// <param name="path"></param>
        /// <returns></returns>
        private async Task<SoftwareBitmap> LoadImage(string path)
        {
            var fs = System.IO.File.OpenRead(path);
            var buf = new byte[fs.Length];
            fs.Read(buf, 0, (int)fs.Length);
            var mem = new MemoryStream(buf);
            mem.Position = 0;

            var stream = await ConvertToRandomAccessStream(mem);
            var bitmap = await LoadImage(stream);
            return bitmap;
        }
        /// <summary>
        /// IRandomAccessStream から SoftwareBitmap を取得
        /// </summary>
        /// <param name="stream"></param>
        /// <returns></returns>
        private async Task<SoftwareBitmap> LoadImage(IRandomAccessStream stream)
        {
            var decoder = await Windows.Graphics.Imaging.BitmapDecoder.CreateAsync(stream);
            var bitmap = await decoder.GetSoftwareBitmapAsync(BitmapPixelFormat.Bgra8, BitmapAlphaMode.Premultiplied);
            return bitmap;
        }
        /// <summary>
        /// MemoryStream から IRandomAccessStream へ変換
        /// </summary>
        /// <param name="memoryStream"></param>
        /// <returns></returns>
        public async Task<IRandomAccessStream> ConvertToRandomAccessStream(MemoryStream memoryStream)
        {
            var randomAccessStream = new InMemoryRandomAccessStream();

            var outputStream = randomAccessStream.GetOutputStreamAt(0);
            var dw = new DataWriter(outputStream);
            var task = new Task(() => dw.WriteBytes(memoryStream.ToArray()));
            task.Start();
            await task;
            await dw.StoreAsync();
            await outputStream.FlushAsync();
            return randomAccessStream;
        }

        /// <summary>
        /// 画像を選択
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void clickOpen(object sender, RoutedEventArgs e)
        {
            var ofd = new OpenFileDialog();
            ofd.FileName = "";
            ofd.Filter = "Image Files|*.bmp;*.jpg;*.png|All Files (*.*)|*.*";
            if (ofd.ShowDialog() == true)
            {
                screenFile = ofd.FileName;
                var bmp = Bitmap.FromFile(screenFile) as Bitmap;
                image.Source = bmp.ToImageSource();
            }
        }
    }
    public static class BitmapEx
    {
        /// <summary>
        /// Bitmap から BitmapSource へ変換
        /// </summary>
        /// <param name="bmp"></param>
        /// <returns></returns>
        public static System.Windows.Media.ImageSource ToImageSource(this Bitmap bmp)
        {
            BitmapSource bitmapSource = Imaging.CreateBitmapSourceFromHBitmap
                (
                    bmp.GetHbitmap(),
                    IntPtr.Zero,
                    Int32Rect.Empty,
                    BitmapSizeOptions.FromEmptyOptions()
                );
            return bitmapSource;
        }
    }

    /// <summary>
    /// WinRT を await/async するために
    /// refer to http://blog.xin9le.net/entry/2012/11/12/123231
    /// </summary>
    public static class TaskEx
    {
        public static Task<T> AsTask<T>(this IAsyncOperation<T> operation)
        {
            var tcs = new TaskCompletionSource<T>();
            operation.Completed = delegate  //--- コールバックを設定
            {
                switch (operation.Status)   //--- 状態に合わせて完了通知
            {
                    case AsyncStatus.Completed: tcs.SetResult(operation.GetResults()); break;
                    case AsyncStatus.Error: tcs.SetException(operation.ErrorCode); break;
                    case AsyncStatus.Canceled: tcs.SetCanceled(); break;
                }
            };
            return tcs.Task;  //--- 完了が通知されるTaskを返す
        }
        public static TaskAwaiter<T> GetAwaiter<T>(this IAsyncOperation<T> operation)
        {
            return operation.AsTask().GetAwaiter();
        }
    }
}

