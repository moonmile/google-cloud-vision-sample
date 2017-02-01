using Google.Cloud.Vision.V1;
using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Runtime.InteropServices;
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

namespace GoogleOCR
{
    /// <summary>
    /// MainWindow.xaml の相互作用ロジック
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
            this.Loaded += MainWindow_Loaded;
        }

        private void MainWindow_Loaded(object sender, RoutedEventArgs e)
        {
            Environment.SetEnvironmentVariable("GOOGLE_APPLICATION_CREDENTIALS", @"D:\work\blog\src\Google\text-detect-test-234e9086ef06.json");
        }

        string screenFile = "screen.png";

        private void clickDetect(object sender, RoutedEventArgs e)
        {
            // OCR認識
            var lst = DetectText(screenFile);
            // 認識した個所をマークする
            var bmp = Bitmap.FromFile(screenFile) as Bitmap;
            var g = Graphics.FromImage(bmp);
            var br = new SolidBrush(System.Drawing.Color.FromArgb(0x80, System.Drawing.Color.Blue));
            var text = "";
            foreach ( var it in lst )
            {
                g.FillRectangle(br, it.Rect);
                g.DrawRectangle(Pens.Red, it.Rect);
                text += it.Text + " ";
            }
            image.Source = bmp.ToImageSource();
            text1.Text = text;
        }


        private List<DetectResult> DetectText(string filename)
        {
            var client = ImageAnnotatorClient.Create();
            var image = Google.Cloud.Vision.V1.Image.FromFile(filename);
            var response = client.DetectText(image);

            var lst = new List<DetectResult>();

            foreach (var annotation in response)
            {
                if (annotation.Description != null)
                    Debug.WriteLine("{0} {1},{2} {3}",
                        annotation.BoundingPoly.Vertices.Count,
                        annotation.BoundingPoly.Vertices[0].X,
                        annotation.BoundingPoly.Vertices[0].Y,
                        annotation.Description);

                var r = new DetectResult
                {
                    Rect = new System.Drawing.Rectangle()
                    {
                        X = annotation.BoundingPoly.Vertices[0].X,
                        Y = annotation.BoundingPoly.Vertices[0].Y,
                        Width = annotation.BoundingPoly.Vertices[2].X - annotation.BoundingPoly.Vertices[0].X,
                        Height = annotation.BoundingPoly.Vertices[2].Y - annotation.BoundingPoly.Vertices[0].Y
                    },
                    Text = annotation.Description
                };
                lst.Add(r);
            }
            // 最初だけ削る
            lst.RemoveAt(0);
            return lst; 
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

    public class DetectResult
    {
        public System.Drawing.Rectangle Rect { get; set; }
        public string Text { get; set; }
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
}
