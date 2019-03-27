using IronOcr;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Timers;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Forms;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace AutoTranslateWindow {
	/// <summary>
	/// Interaction logic for MainWindow.xaml
	/// </summary>
	public partial class MainWindow : Window {

		private System.Timers.Timer UpdateTimer;
		private AutoOcr ocr;
		private Bitmap bitmap;

		private int leftX = -2560;
		private int rightX = 0;
		private int topY = 0;
		private int bottomY = 1440;

		float scale = 1.0f;

		public MainWindow() {
			InitializeComponent();

			UpdateTimer = new System.Timers.Timer(33.3);
			UpdateTimer.Elapsed += UpdateTimer_Elapsed;
			UpdateTimer.Start();

			LeftXIntegerUpDown.Value = leftX;
			TopYIntegerUpDown.Value = topY;
			RightXIntegerUpDown.Value = rightX;
			BottomYIntegerUpDown.Value = bottomY;

			ocr = new AutoOcr();
			//ocr.Language = IronOcr.Languages.Japanese.OcrLanguagePack;
			ocr.Language = IronOcr.Languages.English.OcrLanguagePack;
			bitmap = new Bitmap(rightX - leftX, bottomY - topY, System.Drawing.Imaging.PixelFormat.Format32bppArgb);
		}

		private void UpdateTimer_Elapsed(object sender, ElapsedEventArgs e) {
			UpdateTimer.Stop();

			var workingBitmap = new Bitmap(rightX - leftX, bottomY - topY, System.Drawing.Imaging.PixelFormat.Format32bppArgb);
			var gfx = Graphics.FromImage(workingBitmap);

			gfx.CopyFromScreen(leftX, topY, 0, 0, new System.Drawing.Size(rightX - leftX, bottomY - topY));

			bitmap = ResizeImage(workingBitmap, (int)((rightX - leftX) / scale), (int)((bottomY - topY) / scale));

			var ocrResult = ocr.Read(bitmap);

			string translatedString = "";

			if (ocrResult.Text.Length > 0) {
				try {

					using (var wc = new WebClient()) {
						string s = wc.DownloadString($"https://translate.googleapis.com/translate_a/single?client=gtx&sl=jp&tl=en&dt=t&q={Uri.EscapeDataString(ocrResult.Text)}");
						translatedString = s.Substring(1, 4) != "null" ? s.Split('"')[1] : "";
					}
				} catch (Exception exc) {
					Dispatcher.Invoke(() => {
						ImageView.Source = ToImageSource(bitmap, ImageFormat.Bmp);
						InputLanguageTextBlock.Text = ocrResult.Text;
						OutputLanguageTextBlock.Text = exc.ToString() + $"\nhttps://translate.googleapis.com/translate_a/single?client=gtx&sl=jp&tl=en&dt=t&q={Uri.EscapeDataString(ocrResult.Text)}";
					});
					UpdateTimer.Start();
					return;
				}
			}


			Dispatcher.Invoke(() => {
				ImageView.Source = ToImageSource(bitmap, ImageFormat.Bmp);
				InputLanguageTextBlock.Text = ocrResult.Text;
				OutputLanguageTextBlock.Text = translatedString;
			});
			UpdateTimer.Start();
		}
		public static Bitmap ResizeImage(System.Drawing.Image image, int width, int height) {
			var destRect = new System.Drawing.Rectangle(0, 0, width, height);
			var destImage = new Bitmap(width, height);

			destImage.SetResolution(image.HorizontalResolution, image.VerticalResolution);

			using (var graphics = Graphics.FromImage(destImage)) {
				graphics.CompositingMode = CompositingMode.SourceCopy;
				graphics.CompositingQuality = CompositingQuality.HighQuality;
				graphics.InterpolationMode = InterpolationMode.HighQualityBicubic;
				graphics.SmoothingMode = SmoothingMode.HighQuality;
				graphics.PixelOffsetMode = PixelOffsetMode.HighQuality;

				using (var wrapMode = new ImageAttributes()) {
					wrapMode.SetWrapMode(WrapMode.TileFlipXY);
					graphics.DrawImage(image, destRect, 0, 0, image.Width, image.Height, GraphicsUnit.Pixel, wrapMode);
				}
			}

			return destImage;
		}

		public static ImageSource ToImageSource(System.Drawing.Image image, ImageFormat imageFormat) {
			BitmapImage bitmap = new BitmapImage();

			using (MemoryStream stream = new MemoryStream()) {
				// Save to the stream
				image.Save(stream, imageFormat);

				// Rewind the stream
				stream.Seek(0, SeekOrigin.Begin);

				// Tell the WPF BitmapImage to use this stream
				bitmap.BeginInit();
				bitmap.StreamSource = stream;
				bitmap.CacheOption = BitmapCacheOption.OnLoad;
				bitmap.EndInit();
			}

			return bitmap;
		}

		private void IntegerUpDown_ValueChanged(object sender, RoutedPropertyChangedEventArgs<object> e) {
			leftX = (int)e.NewValue;
			bitmap = new Bitmap(rightX - leftX, bottomY - topY, System.Drawing.Imaging.PixelFormat.Format32bppArgb);
		}

		private void IntegerUpDown_ValueChanged_1(object sender, RoutedPropertyChangedEventArgs<object> e) {
			topY = (int)e.NewValue;
			bitmap = new Bitmap(rightX - leftX, bottomY - topY, System.Drawing.Imaging.PixelFormat.Format32bppArgb);
		}

		private void IntegerUpDown_ValueChanged_2(object sender, RoutedPropertyChangedEventArgs<object> e) {
			rightX = (int)e.NewValue;
			bitmap = new Bitmap(rightX - leftX, bottomY - topY, System.Drawing.Imaging.PixelFormat.Format32bppArgb);
		}

		private void IntegerUpDown_ValueChanged_3(object sender, RoutedPropertyChangedEventArgs<object> e) {
			bottomY = (int)e.NewValue;
			bitmap = new Bitmap(rightX - leftX, bottomY - topY, System.Drawing.Imaging.PixelFormat.Format32bppArgb);
		}
	}
}
