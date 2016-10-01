using System;
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

		public Task<ImageCaptureResult> Capture()
		{
			int id = NextId();

			var pickerIntent = new Intent(Android.App.Application.Context, typeof(ImageCaptureActivity));
			pickerIntent.PutExtra(ImageCaptureActivity.ExtraId, id);
			pickerIntent.SetFlags(ActivityFlags.NewTask);
			Android.App.Application.Context.StartActivity(pickerIntent);

			return ImageCaptureDelegateToTask(id);
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
	}
}
