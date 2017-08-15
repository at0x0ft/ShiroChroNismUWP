using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;
using Windows.Storage;
using Windows.Storage.Pickers;
using Windows.Storage.Streams;
using Windows.Graphics.Imaging;
using Windows.UI.Xaml.Media.Imaging;
using System.Runtime.InteropServices;
using System.Threading.Tasks;


// 空白ページの項目テンプレートについては、https://go.microsoft.com/fwlink/?LinkId=402352&clcid=0x411 を参照してください

namespace ShiroChroNism_UWP
{
	/// <summary>
	/// それ自体で使用できる空白ページまたはフレーム内に移動できる空白ページ。
	/// </summary>
	public sealed partial class MainPage : Page
	{
		public MainPage()
		{
			InitializeComponent();
		}

		private SoftwareBitmap Bitmap { get; set; }
		private StorageFile InputFile { get; set; }

		private async Task SetCtrlToImgAsync(Image imgview) // Set the source of the Image control
		{
			if(Bitmap.BitmapPixelFormat != BitmapPixelFormat.Bgra8 || Bitmap.BitmapAlphaMode == BitmapAlphaMode.Straight)
			{
				Bitmap = SoftwareBitmap.Convert(Bitmap, BitmapPixelFormat.Bgra8, BitmapAlphaMode.Premultiplied);
			}

			var source = new SoftwareBitmapSource();
			await source.SetBitmapAsync(Bitmap);

			imgview.Source = source;
		}

		private async void OpenFile_Click(object sender, RoutedEventArgs e)
		{
			// 画像を開くダイアログの設定
			FileOpenPicker fileOpenPicker = new FileOpenPicker() { SuggestedStartLocation = PickerLocationId.PicturesLibrary };
			fileOpenPicker.FileTypeFilter.Add(".jpg");
			fileOpenPicker.ViewMode = PickerViewMode.Thumbnail;

			// ファイルを1つ選択する
			InputFile = await fileOpenPicker.PickSingleFileAsync();
			if(InputFile == null)
			{
				// The user cancelled the picking operation
				return;
			}

			Bitmap = await OutputFunctions.CreateSoftwareBitMapAsyncFrom(InputFile);
			await SetCtrlToImgAsync(Preview);
		}

		private async void GenerateButton_Click(object sender, RoutedEventArgs e)
		{
			if(Bitmap == null)
			{
				OpenFile_Click(sender, e);
				if(Bitmap == null)
				{
					return;
				}
			}
			else
			{
				using(IRandomAccessStream stream = await InputFile.OpenAsync(FileAccessMode.Read))
				{
					// Create the decoder from the stream
					BitmapDecoder decoder = await BitmapDecoder.CreateAsync(stream);

					// Get the SoftwareBitmap representation of the file
					Bitmap = await decoder.GetSoftwareBitmapAsync();
				}

				using(BitmapBuffer buffer = Bitmap.LockBuffer(BitmapBufferAccessMode.Write))
				{
					using(var reference = buffer.CreateReference())
					{
						unsafe
						{
							((IMemoryBufferByteAccess)reference).GetBuffer(out byte* dataInBytes, out uint capacity);

							BitmapPlaneDescription bufferLayout = buffer.GetPlaneDescription(0);
							for(int i = 0; i < bufferLayout.Height; i++)
							{
								for(int j = 0; j < bufferLayout.Width; j++)
								{
									int bw;
									if(geometricMean.IsChecked == true)
									{
										bw = (int)Math.Pow(dataInBytes[bufferLayout.StartIndex + bufferLayout.Stride * i + 4 * j + 0] * dataInBytes[bufferLayout.StartIndex + bufferLayout.Stride * i + 4 * j + 1] * dataInBytes[bufferLayout.StartIndex + bufferLayout.Stride * i + 4 * j + 2], 1.0 / 3);
									}
									else
									{
										bw = dataInBytes[bufferLayout.StartIndex + bufferLayout.Stride * i + 4 * j + 0] + dataInBytes[bufferLayout.StartIndex + bufferLayout.Stride * i + 4 * j + 1] + dataInBytes[bufferLayout.StartIndex + bufferLayout.Stride * i + 4 * j + 2];
										bw /= 3;
									}

									if(bw < bwchanger.Value)
									{
										bw = 0;
									}
									else
									{
										bw = 255;
									}
									dataInBytes[bufferLayout.StartIndex + bufferLayout.Stride * i + 4 * j + 0] = (byte)bw;	// B
									dataInBytes[bufferLayout.StartIndex + bufferLayout.Stride * i + 4 * j + 1] = (byte)bw;	// G
									dataInBytes[bufferLayout.StartIndex + bufferLayout.Stride * i + 4 * j + 2] = (byte)bw;	// R
									// dataInBytes[bufferLayout.StartIndex + bufferLayout.Stride * i + 4 * j + 3] represents A
								}
							}
						}
					}
				}

				await SetCtrlToImgAsync(Afterview);
			}
		}

		public async void SaveFile_Click(object sender, RoutedEventArgs e)
		{
			await OutputFunctions.SaveImgAsync(Bitmap);
		}

		private void TextBox_SizeChanged(object sender, SizeChangedEventArgs e)
		{

		}
	}

	[ComImport]
	[Guid("5B0D3235-4DBA-4D44-865E-8F1D0E4FD04D")]
	[InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
	unsafe interface IMemoryBufferByteAccess
	{
		void GetBuffer(out byte* buffer, out uint capacity);
	}
}