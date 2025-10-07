using System;
using System.Drawing; // System.Drawing.Common in .NET Core/5+ (Windows only)
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.IO;
using System.Reflection.Metadata;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Media.Media3D;
using System.Windows.Shapes;

using Windows.Graphics.Imaging;
using Windows.Media.Ocr;
using Windows.Storage.Streams;

using Clipboard = System.Windows.Clipboard;
using Color = System.Windows.Media.Color;
using Imaging = System.Windows.Interop.Imaging;
using PixelFormat = System.Drawing.Imaging.PixelFormat;
using Point = System.Windows.Point;
using WpfRectangle = System.Windows.Shapes.Rectangle;
//using BitmapDecoder = System.Windows.Media.Imaging.BitmapDecoder;
namespace SnippingCopyTool
{
    public partial class MainWindow : Window
    {

        public MainWindow()
        {
            InitializeComponent();
        }

        private void NewSnipButton_Click(object sender, RoutedEventArgs e)
        {
            var snipWindow = new SnippingWindow(this);
            snipWindow.Show();
            this.Hide();
        }

        private void ExitButton_Click(object sender, RoutedEventArgs e)
        {
            Application.Current.Shutdown();
        }
    }
}
