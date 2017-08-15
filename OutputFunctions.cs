using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Windows.Graphics.Imaging;
using Windows.Storage;
using Windows.Storage.Pickers;
using Windows.Storage.Streams;

namespace ShiroChroNism_UWP
{
	class OutputFunctions
	{
		public static async Task<SoftwareBitmap> CreateSoftwareBitMapAsyncFrom(StorageFile inputFile)
		{
			SoftwareBitmap softwareBitmap;

			using(IRandomAccessStream stream = await inputFile.OpenAsync(FileAccessMode.Read))
			{
				// Create the decoder from the stream
				BitmapDecoder decoder = await BitmapDecoder.CreateAsync(stream);

				// Get the SoftwareBitmap representation of the file

				softwareBitmap = new SoftwareBitmap(decoder.BitmapPixelFormat, (int)decoder.PixelWidth, (int)decoder.PixelHeight);

				softwareBitmap = await decoder.GetSoftwareBitmapAsync();

				return softwareBitmap;
			}
		}

		public static async Task SaveImgAsync(SoftwareBitmap softwareBitmap)
		{
			if(softwareBitmap == null)
			{
				return;
			}

			FileSavePicker fileSavePicker = new FileSavePicker() { SuggestedStartLocation = PickerLocationId.PicturesLibrary };
			fileSavePicker.FileTypeChoices.Add("JPEG files", new List<string>() { ".jpg" });
			fileSavePicker.SuggestedFileName = "image";

			var outputFile = await fileSavePicker.PickSaveFileAsync();

			if(outputFile == null)
			{
				// The user cancelled the picking operation
				return;
			}

			await SaveSoftwareBitmapToFile(softwareBitmap, outputFile);
		}

		private static async Task SaveSoftwareBitmapToFile(SoftwareBitmap softwareBitmap, StorageFile outputFile)
		{
			using(IRandomAccessStream stream = await outputFile.OpenAsync(FileAccessMode.ReadWrite))
			{
				// Create an encoder with the desired format
				BitmapEncoder encoder = await BitmapEncoder.CreateAsync(BitmapEncoder.JpegEncoderId, stream);

				// Set the software bitmap
				encoder.SetSoftwareBitmap(softwareBitmap);

				encoder.BitmapTransform.InterpolationMode = BitmapInterpolationMode.Fant;
				encoder.IsThumbnailGenerated = true;

				try
				{
					await encoder.FlushAsync();
				}
				catch(Exception err)
				{
					switch(err.HResult)
					{
						case unchecked((int)0x88982F81): //WINCODEC_ERR_UNSUPPORTEDOPERATION
														 // If the encoder does not support writing a thumbnail, then try again
														 // but disable thumbnail generation.
							encoder.IsThumbnailGenerated = false;
							break;
						default:
							throw err;
					}
				}

				if(encoder.IsThumbnailGenerated == false)
				{
					await encoder.FlushAsync();
				}
			}
		}
	}
}
