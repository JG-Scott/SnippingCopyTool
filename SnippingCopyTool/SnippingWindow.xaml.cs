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


    namespace SnippingCopyTool
{

        public partial class SnippingWindow : Window
        {
            private System.Windows.Point start;
            private bool dragging = false;
            private bool HasCaptureArea = false;
            private MainWindow _mainWindow;
            public SnippingWindow(MainWindow mainWindow)
            {
                InitializeComponent();
                _mainWindow = mainWindow;
                // Capture ESC to cancel
                this.KeyDown += Window_KeyDown;
            }

            private void Window_MouseDown(object sender, MouseButtonEventArgs e)
            {
                if (HasCaptureArea) { return; }
                if (e.ChangedButton == MouseButton.Left)
                {
                    start = e.GetPosition(this);
                    SelRect.Width = 0;
                    SelRect.Height = 0;
                    Canvas.SetLeft(SelRect, start.X);
                    Canvas.SetTop(SelRect, start.Y);
                    SelRect.Visibility = Visibility.Visible;
                    dragging = true;
                    Mouse.Capture(this);
                }
            }

            private void Window_MouseMove(object sender, MouseEventArgs e)
            {
                if (HasCaptureArea) { return; }
                if (!dragging) return;
                var p = e.GetPosition(this);

                DrawCaptureRectangle(p);
            }

            void DrawCaptureRectangle(Point p)
            {
                double x = Math.Min(p.X, start.X);
                double y = Math.Min(p.Y, start.Y);
                double w = Math.Abs(p.X - start.X);
                double h = Math.Abs(p.Y - start.Y);

                Canvas.SetLeft(SelRect, x);
                Canvas.SetTop(SelRect, y);
                SelRect.Width = w;
                SelRect.Height = h;
            }

            // Holding Select Rect Until Button Press
            private void Window_MouseUp(object sender, MouseButtonEventArgs e)
            {
                if (HasCaptureArea) { return; }
                if (!dragging) return;
                dragging = false;
                Mouse.Capture(null);

                var end = e.GetPosition(this);
                var rect = new System.Windows.Rect(start, end);

                // Hide overlay before capturing to avoid capturing the overlay window itself

                try
                {
                    // Convert WPF units to device pixels (account for DPI scaling)
                    var dpiFactor = GetDpiScale();
                    int x = (int)Math.Round(rect.X * dpiFactor);
                    int y = (int)Math.Round(rect.Y * dpiFactor);
                    int width = (int)Math.Round(rect.Width * dpiFactor);
                    int height = (int)Math.Round(rect.Height * dpiFactor);

                    if (width <= 0 || height <= 0) { this.Show(); return; }
                    HasCaptureArea = true;

                    PlaceTransformButtons(x, y, width, height);

                }
                catch (Exception ex)
                {
                    MessageBox.Show("Capture failed: " + ex.Message);
                }

            }

            void TransformHandlesHoverEnter(object sender, MouseEventArgs e)
            {
                if (sender is WpfRectangle handle)
                {
                    handle.Fill = System.Windows.Media.Brushes.Red;

                }
            }

            void TransformHandlesHoverExit(object sender, MouseEventArgs e)
            {
                if (sender is WpfRectangle handle)
                {
                    handle.Fill = System.Windows.Media.Brushes.Cyan;

                }
            }

            void PlaceTransformButtons(int x, int y, int width, int height)
            {
                int TransformButtonSize = 20;
                float TransformButtonOffset = TransformButtonSize / 2;


                Canvas.SetTop(TopLeft, y - TransformButtonOffset);
                Canvas.SetLeft(TopLeft, x - TransformButtonOffset);
                TopLeft.Width = TransformButtonSize;
                TopLeft.Height = TransformButtonSize;


                Canvas.SetTop(TopRight, y - TransformButtonOffset);
                Canvas.SetLeft(TopRight, x + width - TransformButtonOffset);
                TopRight.Width = TransformButtonSize;
                TopRight.Height = TransformButtonSize;


                Canvas.SetTop(BottomLeft, y + height - TransformButtonOffset);
                Canvas.SetLeft(BottomLeft, x - TransformButtonOffset);
                BottomLeft.Width = TransformButtonSize;
                BottomLeft.Height = TransformButtonSize;

                Canvas.SetTop(BottomRight, y + height - TransformButtonOffset);
                Canvas.SetLeft(BottomRight, x + width - TransformButtonOffset);
                BottomRight.Width = TransformButtonSize;
                BottomRight.Height = TransformButtonSize;

            }

            async void CopyFromScreen()
            {
                int x = (int)Canvas.GetLeft(SelRect);
                int y = (int)Canvas.GetTop(SelRect);
                int width = (int)SelRect.Width;
                int height = (int)SelRect.Height;
                this.Hide();
                System.Threading.Thread.Sleep(50); // short pause so OS renders hide
                using (var bmp = new Bitmap(width, height, System.Drawing.Imaging.PixelFormat.Format32bppPArgb))
                {
                    using (var g = Graphics.FromImage(bmp))
                    {
                        g.CopyFromScreen(x - 10, y - 10, 0, 0, new System.Drawing.Size(width + 10, height + 10), CopyPixelOperation.SourceCopy);
                    }
                       await CaptureNumbersFromScreenShot(bmp);
                }
            _mainWindow.Show();
            this.Close();
        }

            //public static Bitmap IncreaseDpi(Bitmap input, float scale = 2.0f)
            //{
            //    int newWidth = (int)(input.Width * scale);
            //    int newHeight = (int)(input.Height * scale);

            //    // Create a new higher-resolution bitmap
            //    Bitmap highDpiBitmap = new Bitmap(newWidth, newHeight);
            //    highDpiBitmap.SetResolution(input.HorizontalResolution * scale, input.VerticalResolution * scale);
                        
            //    using (Graphics g = Graphics.FromImage(highDpiBitmap))
            //    {
            //        g.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.HighQualityBicubic;
            //        g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.HighQuality;
            //        g.PixelOffsetMode = System.Drawing.Drawing2D.PixelOffsetMode.HighQuality;
            //        g.CompositingQuality = System.Drawing.Drawing2D.CompositingQuality.HighQuality;

            //        g.DrawImage(input, new System.Drawing.Rectangle(0, 0, newWidth, newHeight));
            //    }
            //    //input.Dispose();
            //    return highDpiBitmap;
            //}
            //public static Bitmap AddPadding(Bitmap input, int padding = 40)
            //{
            //    Bitmap padded = new Bitmap(input.Width + padding * 2, input.Height + padding * 2);
            //    //input.Dispose();
            //    using (Graphics g = Graphics.FromImage(padded))
            //    {
            //        g.Clear(System.Drawing.Color.White);
            //        g.DrawImage(input, padding, padding);
            //    }
            //    return padded;
            //}

            async Task CaptureNumbersFromScreenShot(Bitmap BMP)
            {
                //Bitmap padding = AddPadding(BMP, 40);
                //Bitmap processed = IncreaseDpi(padding, 3);

                string recognizedText = await ExtractNumbersFromBitmapAsync(BMP);

                Clipboard.SetText(recognizedText);
                if (string.IsNullOrWhiteSpace(recognizedText))
                    MessageBox.Show("No text detected.");
                else
                    MessageBox.Show($"Detected text:\n{recognizedText}");
            }

            public static async Task<string> ExtractNumbersFromBitmapAsync(Bitmap bitmap)
            {
                if (bitmap == null) return string.Empty;

                using (var memoryStream = new MemoryStream())
                {
                    // Convert the Bitmap to PNG in-memory
                    bitmap.Save(memoryStream, System.Drawing.Imaging.ImageFormat.Png);
                    memoryStream.Position = 0;

                    // Convert to WinRT stream
                    using (IRandomAccessStream stream = new InMemoryRandomAccessStream())
                    {
                        await stream.WriteAsync(memoryStream.ToArray().AsBuffer());
                        stream.Seek(0);

                        // Decode the image into a SoftwareBitmap
                        var decoder = await Windows.Graphics.Imaging.BitmapDecoder.CreateAsync(stream);
                        SoftwareBitmap softwareBitmap = await decoder.GetSoftwareBitmapAsync();
                        try
                        {
                            // Ensure format is compatible with OCR (Gray8 or Bgra8)
                            if (softwareBitmap.BitmapPixelFormat != BitmapPixelFormat.Bgra8 &&
                                softwareBitmap.BitmapPixelFormat != BitmapPixelFormat.Gray8)
                            {
                                softwareBitmap = SoftwareBitmap.Convert(softwareBitmap, BitmapPixelFormat.Bgra8);
                            }

                            // Run OCR
                            var ocrEngine = OcrEngine.TryCreateFromLanguage(new Windows.Globalization.Language("en"));
                            var ocrResult = await ocrEngine.RecognizeAsync(softwareBitmap);
                            // Optional: Extract only numbers
                            var matches = Regex.Matches(ocrResult.Text, @"\d+");

                            string numbersOnly = string.Join("\n", matches.Select(m => m.Value));
                            return numbersOnly;
                        }
                        finally 
                        {

                        softwareBitmap.Dispose();
                        }
                     
                    }
                }
            }

            private void Window_KeyDown(object sender, KeyEventArgs e)
            {
                if (e.Key == Key.Escape) Close();
            }

            private double GetDpiScale()
            {
                // convert WPF units (96dpi-based) to actual device pixels
                var source = PresentationSource.FromVisual(this);
                if (source?.CompositionTarget != null)
                {
                    return 96.0 * source.CompositionTarget.TransformToDevice.M11 / 96.0; // effectively M11
                }
                return 1.0;
            }

            void OnClick2(object sender, RoutedEventArgs e)
            {
                CopyFromScreen();

            }


            private bool resizing = false;
            private System.Windows.Point resizeStart;
            private double initialWidth;
            private double initialHeight;
            private double initialTop;
            private double initialLeft;
            void HandleBR_MouseDown(object sender, MouseButtonEventArgs e)
            {
                e.Handled = true;
                resizing = true;
                resizeStart = e.GetPosition(RootCanvas);
                initialWidth = SelRect.Width;
                initialHeight = SelRect.Height;
                initialTop = Canvas.GetTop(SelRect);
                initialLeft = Canvas.GetLeft(SelRect);

                Mouse.Capture((UIElement)sender);
            }
            void HandleBR_MouseUp(object sender, MouseButtonEventArgs e)
            {
                resizing = false;
                Mouse.Capture(null);
            }
            void HandleBR_MouseMove(object sender, MouseEventArgs e)
            {
                if (!resizing) return;

                var current = e.GetPosition(RootCanvas);
                double deltaX = current.X - resizeStart.X;
                double deltaY = current.Y - resizeStart.Y;


                if (sender is WpfRectangle handle)
                {

                    var tag = handle.Tag as String;
                    if (tag != null)
                    {
                        switch (tag)
                        {
                            case "TL":
                                SelRect.Width = Math.Max(1, initialWidth - deltaX);
                                SelRect.Height = Math.Max(1, initialHeight - deltaY);
                                Canvas.SetTop(SelRect, initialTop + deltaY);
                                Canvas.SetLeft(SelRect, initialLeft + deltaX);
                                break;
                            case "TR":
                                //Top right movement
                                SelRect.Width = Math.Max(1, initialWidth + deltaX);
                                SelRect.Height = Math.Max(1, initialHeight - deltaY);
                                Canvas.SetTop(SelRect, initialTop + deltaY);
                                break;
                            case "BL":
                                // BOTTOM LEFT MOVEMENT
                                SelRect.Width = Math.Max(1, initialWidth - deltaX);
                                SelRect.Height = Math.Max(1, initialHeight + deltaY);
                                Canvas.SetLeft(SelRect, initialLeft + deltaX);
                                break;
                            case "BR":
                                //Bottom Right Movement
                                SelRect.Width = Math.Max(1, initialWidth + deltaX);
                                SelRect.Height = Math.Max(1, initialHeight + deltaY);
                                break;
                        }
                    }
                }

                // Move handle to new position (bottom-right corner)
                double x = Canvas.GetLeft(SelRect);
                double y = Canvas.GetTop(SelRect);
                PlaceTransformButtons((int)x, (int)y, (int)SelRect.Width, (int)SelRect.Height);
            }
        }





        internal static class NativeMethods
        {
            [System.Runtime.InteropServices.DllImport("gdi32.dll")]
            internal static extern bool DeleteObject(IntPtr hObject);
        }
    }

