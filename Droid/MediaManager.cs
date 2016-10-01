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

[assembly: Dependency(typeof(MediaManager))]
namespace Camera.Droid
{
	public class MediaPickedEventArgs: EventArgs
	{
		public MediaPickedEventArgs(int id, Exception error)
		{
			if (error == null)
				throw new ArgumentNullException("error");

			RequestId = id;
			Error = error;
		}

		public MediaPickedEventArgs(int id, bool isCanceled, string path = null)
		{
			RequestId = id;
			IsCanceled = isCanceled;

			if (!IsCanceled && path == null)
				throw new ArgumentNullException("path");

			Path = path;
		}

		public int RequestId
		{
			get;
			private set;
		}

		public bool IsCanceled
		{
			get;
			private set;
		}

		public Exception Error
		{
			get;
			private set;
		}

		public string Path
		{
			get;
			private set;
		}
	}

	public class MediaManager: IMediaManager
	{
		private int _requestId;
		private TaskCompletionSource<string> _completionSource;

        private int GetRequestId()
		{
			int id = _requestId;
			if (_requestId == Int32.MaxValue)
				_requestId = 0;
			else
				_requestId++;

			return id;
		}

		public Task<string> TakePicture()
		{
			int id = GetRequestId();

			var options = new StoreCameraMediaOptions {
				Directory = "test",
				Name = "test_photo.jpg",
				SaveToAlbum = true
			};

			Android.App.Application.Context.StartActivity(CreateMediaIntent(id, "image/*", MediaStore.ActionImageCapture, options));

			var ntcs = new TaskCompletionSource<string>(id);
			if (Interlocked.CompareExchange(ref _completionSource, ntcs, null) != null)
				throw new InvalidOperationException("Only one operation can be active at a time");

			EventHandler<MediaPickedEventArgs> handler = null;
			handler = (s, e) =>
			{
				var tcs = Interlocked.Exchange(ref _completionSource, null);

				MediaPickerActivity.MediaPicked -= handler;

				if (e.RequestId != id)
					return;

				if (e.IsCanceled)
					tcs.SetResult(null);
				else if (e.Error != null)
					tcs.SetException(e.Error);
				else
					tcs.SetResult(e.Path);
			};

			MediaPickerActivity.MediaPicked += handler;

			return _completionSource.Task;
		}

		private Intent CreateMediaIntent(int id, string type, string action, StoreCameraMediaOptions options)
		{
			var pickerIntent = new Intent(Android.App.Application.Context, typeof(MediaPickerActivity));
			pickerIntent.PutExtra(MediaPickerActivity.ExtraId, id);
			pickerIntent.PutExtra(MediaPickerActivity.ExtraType, type);
			pickerIntent.PutExtra(MediaPickerActivity.ExtraPath, options.Directory);
			pickerIntent.PutExtra(MediaPickerActivity.ExtraSaveToAlbum, options.SaveToAlbum);
			pickerIntent.SetFlags(ActivityFlags.ClearTop);
			pickerIntent.SetFlags(ActivityFlags.NewTask);
			return pickerIntent;
		}
	}
}
