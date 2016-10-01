using System;
using Android.App;
using Android.Runtime;
using Android.Content.PM;
using Android.Media;
using Android.Net;
using Android.OS;
using Android.Provider;
using Android.Telecom;
using Android.Content;
using Environment = Android.OS.Environment;
using Path = System.IO.Path;
using Uri = Android.Net.Uri;
using System.IO;
using System.Threading.Tasks;
using Android.Database;
using System.Threading;

namespace Camera.Droid
{
	[Activity(ConfigurationChanges = ConfigChanges.Orientation | ConfigChanges.ScreenSize)]
	public class ImageCaptureActivity : Activity
	{
		internal static event EventHandler<ImageCaptureEventArgs> ImageCaptured;
		internal const string ExtraId = "id";
		internal const string ExtraPath = "path";
		internal const string ExtraSaveToAlbum = "album_save";

		const string Action = MediaStore.ActionImageCapture;
		int _id;
		string _title;
		string _extraPath;
		Uri _path;

		protected override void OnCreate(Bundle savedInstanceState)
		{
			base.OnCreate(savedInstanceState);

			Bundle b = (savedInstanceState ?? Intent.Extras);
			_id = b.GetInt(ExtraId, 0);

			// Define name of the file
			_title = "IMG_" + DateTime.Now.ToString("yyyyMMdd_HHmmssfff") + ".jpg";

			// Define subfolder which will contain all the files
			_extraPath = "camera_test";

			var imageCaptureIntent = new Intent(Action);
			imageCaptureIntent.PutExtra(ExtraSaveToAlbum, true);
			try
			{
				using (Java.IO.File mediaStorageDir = new Java.IO.File(Environment.GetExternalStoragePublicDirectory(Environment.DirectoryPictures), _extraPath))
				{
					if (!mediaStorageDir.Exists())
					{
						var result = mediaStorageDir.Mkdirs();
						if (!result) throw new IOException("Failed to create directory, make sure WRITE_EXTERNAL_STORAGE permission is set.");
					}

					// Path needs to be expressed as a URI to be passed to the image capture intent
					_path = Uri.FromFile(new Java.IO.File(Path.Combine(mediaStorageDir.Path, _title)));
					File.Create(_path.Path).Close();
					imageCaptureIntent.PutExtra(MediaStore.ExtraOutput, _path);
				}

				// Starts the image capture intent given the request id.
				StartActivityForResult(imageCaptureIntent, _id);
			}
			catch (Exception ex)
			{
				TriggerImageCapture(this, ImageCaptureEventArgs.CreateError(_id, ex));
			}
			finally
			{
				imageCaptureIntent.Dispose();
			}
		}

		protected override void OnActivityResult(int requestCode, Result resultCode, Intent data)
		{
			base.OnActivityResult(requestCode, resultCode, data);

			if (requestCode != _id || resultCode == Result.Canceled)
			{
				File.Delete(_path.Path);
				TriggerImageCapture(this, ImageCaptureEventArgs.CreateaCancelled(_id));
			}
			else
			{
				TriggerImageCapture(this, ImageCaptureEventArgs.CreateSuccess(_id, _path.Path));
			}

			Finish();
		}

		protected override void OnSaveInstanceState(Bundle outState)
		{
			outState.PutString(MediaStore.MediaColumns.Title, _title);
			outState.PutInt(ExtraId, _id);
			outState.PutString(ExtraPath, _extraPath);
			base.OnSaveInstanceState(outState);
		}

		static void TriggerImageCapture(object sender, ImageCaptureEventArgs eventArgs)
		{
			if (eventArgs != null)
				ImageCaptured(sender, eventArgs);
		}
	}

	class ImageCaptureEventArgs : EventArgs
	{
		private ImageCaptureEventArgs(int id, Exception error, bool isCanceled, string path)
		{
			RequestId = id;
			Error = error;
			IsCanceled = isCanceled;
			Path = path;
		}

		public static ImageCaptureEventArgs CreateError(int id, Exception error)
		{
			return new ImageCaptureEventArgs(id, error, false, null);
		}

		public static ImageCaptureEventArgs CreateaCancelled(int id)
		{
			return new ImageCaptureEventArgs(id, null, true, null);
		}

		public static ImageCaptureEventArgs CreateSuccess(int id, string path)
		{
			return new ImageCaptureEventArgs(id, null, false, path);
		}

		public int RequestId { get; private set; }
		public bool IsCanceled { get; private set; }
		public Exception Error { get; private set; }
		public string Path { get; private set; }
	}
}
