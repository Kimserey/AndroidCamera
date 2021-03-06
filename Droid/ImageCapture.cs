﻿using System;
using System.Threading.Tasks;
using Android.Content;
using Android.Content.PM;
using Android.OS;
using Android.Provider;
using Android.Media;
using Android.Graphics;
using Android.App;
using Android.Hardware.Camera2;
using System.Threading;
using Xamarin.Forms;
using Camera.Droid;
using System.IO;

[assembly: Dependency(typeof(ImageCapture))]
namespace Camera.Droid
{
	public class ImageCapture : IImageCapture
	{
		// Id used to ensure unicity of the request
		// and also used in activity to match OnCreate and OnActivityResult.
		int _id;
		int NextId()
		{
			int id = _id;
			if (_id == Int32.MaxValue) _id = 0;
			else _id++;
			return id;
		}

		public async Task<ImageCaptureResult> Capture()
		{
			int id = NextId();

			var pickerIntent = new Intent(Android.App.Application.Context, typeof(ImageCaptureActivity));
			pickerIntent.PutExtra(ImageCaptureActivity.ExtraId, id);
			pickerIntent.SetFlags(ActivityFlags.NewTask);
			Android.App.Application.Context.StartActivity(pickerIntent);

			var result = await ImageCaptureDelegateToTask(id);
			await FixExif(result.Path);

			return result;
		}

		// Shared task completion source used to track camera instances running
		// and handle return results.
		TaskCompletionSource<ImageCaptureResult> _completionSource;

		// Transforms the asynchronous delegate + handler code to a task
		// using task completion source.
		public Task<ImageCaptureResult> ImageCaptureDelegateToTask(int id)
		{
			var next = new TaskCompletionSource<ImageCaptureResult>(id);
			if (Interlocked.CompareExchange(ref _completionSource, next, null) != null)
				throw new InvalidOperationException("Another task is already started.");

			EventHandler<ImageCaptureEventArgs> handler = null;

			handler = (s, e) =>
			{
				var tcs = Interlocked.Exchange(ref _completionSource, null);

				ImageCaptureActivity.ImageCaptured -= handler;

				if (e.RequestId != id)
					return;

				if (e.IsCanceled)
					tcs.SetResult(new ImageCaptureResult { Success = false });
				else if (e.Error != null)
					tcs.SetException(e.Error);
				else
					tcs.SetResult(new ImageCaptureResult { Success = true, Path = e.Path });
			};

			ImageCaptureActivity.ImageCaptured += handler;

			return _completionSource.Task;
		}

		// Fixes the orientation of the file.
		public Task FixExif(string filePath)
		{
			return Task.Run(() =>
			{
				var orientation = 0;

				using (var ei = new ExifInterface(filePath))
				{
					switch ((Orientation)ei.GetAttributeInt(ExifInterface.TagOrientation, (int)Orientation.Normal))
					{
						case Orientation.Rotate90:
							orientation = 90; break;
						case Orientation.Rotate180:
							orientation = 180; break;
						case Orientation.Rotate270:
							orientation = 270; break;
						default:
							orientation = 0; break;
					}
				}

				if (orientation != 0)
				{
					using (var originalImage = BitmapFactory.DecodeFile(filePath))
					{
						var matrix = new Matrix();
						matrix.PostRotate(orientation);
						using (var rotatedImage = Bitmap.CreateBitmap(originalImage, 0, 0, originalImage.Width, originalImage.Height, matrix, true))
						{
							using (var stream = File.Open(filePath, FileMode.Create, FileAccess.ReadWrite))
							{
								rotatedImage.Compress(Bitmap.CompressFormat.Jpeg, 100, stream);
								stream.Close();
							}
							rotatedImage.Recycle();
						}
						originalImage.Recycle();
						GC.Collect();
					}
				}
			});
		}

	}
}
