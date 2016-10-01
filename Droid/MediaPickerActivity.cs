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

namespace Camera.Droid
{
	[Activity(ConfigurationChanges = ConfigChanges.Orientation | ConfigChanges.ScreenSize)]
	[Preserve(AllMembers = true)]
	public class MediaPickerActivity: Activity
	{
		internal const string ExtraPath = "path";
		internal const string ExtraLocation = "location";
		internal const string ExtraType = "type";
		internal const string ExtraId = "id";
		internal const string ExtraAction = "action";
		internal const string ExtraTasked = "tasked";
		internal const string ExtraSaveToAlbum = "album_save";
		internal const string ExtraFront = "android.intent.extras.CAMERA_FACING";

        internal static event EventHandler<MediaPickedEventArgs> MediaPicked;

		private int id;
		private int front;
		private string title;
		private string description;
		private string type;
		private Uri path;
		private bool isPhoto;
		private bool saveToAlbum;
		private string action;

		private int seconds;
		private VideoQuality quality;
		private bool tasked;

		protected override void OnCreate(Bundle savedInstanceState)
		{
			base.OnCreate(savedInstanceState);

			Bundle b = (savedInstanceState ?? Intent.Extras);

			bool ran = b.GetBoolean("ran", defaultValue: false);

			this.title = b.GetString(MediaStore.MediaColumns.Title);
			this.description = b.GetString(MediaStore.Images.ImageColumns.Description);

			this.id = b.GetInt(ExtraId, 0);
			this.type = b.GetString(ExtraType);
			this.front = b.GetInt(ExtraFront);

			if (this.type == "image/*")
				this.isPhoto = true;

			this.action = b.GetString(ExtraAction);
			Intent pickIntent = null;
			try
			{
				pickIntent = new Intent(this.action);
				if (this.action == Intent.ActionPick)
					pickIntent.SetType(type);
				else
				{
					if (!this.isPhoto)
					{
						this.seconds = b.GetInt(MediaStore.ExtraDurationLimit, 0);
						if (this.seconds != 0)
							pickIntent.PutExtra(MediaStore.ExtraDurationLimit, seconds);
					}

					this.saveToAlbum = b.GetBoolean(ExtraSaveToAlbum);
					pickIntent.PutExtra(ExtraSaveToAlbum, this.saveToAlbum);

					this.quality = (VideoQuality)b.GetInt(MediaStore.ExtraVideoQuality, (int)VideoQuality.High);
					pickIntent.PutExtra(MediaStore.ExtraVideoQuality, GetVideoQuality(this.quality));

					if (front != 0)
						pickIntent.PutExtra(ExtraFront, (int)Android.Hardware.CameraFacing.Front);

					if (!ran)
					{
						this.path = GetOutputMediaFile(this, b.GetString(ExtraPath), this.title, this.isPhoto, false);

						Touch();
						pickIntent.PutExtra(MediaStore.ExtraOutput, this.path);
					}
					else
						this.path = Uri.Parse(b.GetString(ExtraPath));
				}

				if (!ran)
					StartActivityForResult(pickIntent, this.id);
			}
			catch (Exception ex)
			{
				OnMediaPicked(new MediaPickedEventArgs(this.id, ex));
			}
			finally
			{
				if (pickIntent != null)
					pickIntent.Dispose();
			}
		}

		protected override async void OnActivityResult(int requestCode, Result resultCode, Intent data)
		{
			base.OnActivityResult(requestCode, resultCode, data);

			//if (this.tasked)
			//{
			//	Task<MediaPickedEventArgs> future;

			//	if (resultCode == Result.Canceled)
			//	{
			//		//delete empty file
			//		DeleteOutputFile();

			//		future = TaskFromResult(new MediaPickedEventArgs(requestCode, isCanceled: true));

			//		Finish();

			//		future.ContinueWith(t => OnMediaPicked(t.Result));
			//	}
			//	else
			//	{
			//		if ((int)Build.VERSION.SdkInt >= 22)
			//		{
			//			var e = await GetMediaFileAsync(this, requestCode, this.action, this.isPhoto, ref this.path, (data != null) ? data.Data : null, false);
			//			OnMediaPicked(e);
			//			Finish();
			//		}
			//		else
			//		{
			//			future = GetMediaFileAsync(this, requestCode, this.action, this.isPhoto, ref this.path, (data != null) ? data.Data : null, false);

			//			Finish();

			//			future.ContinueWith(t => OnMediaPicked(t.Result));
			//		}
			//	}
			//}
			//else
			//{
			//	if (resultCode == Result.Canceled)
			//	{
			//		//delete empty file
			//		DeleteOutputFile();

			//		SetResult(Result.Canceled);
			//	}
			//	else
			//	{
			//		Intent resultData = new Intent();
			//		resultData.PutExtra("MediaFile", (data != null) ? data.Data : null);
			//		resultData.PutExtra("path", this.path);
			//		resultData.PutExtra("isPhoto", this.isPhoto);
			//		resultData.PutExtra("action", this.action);
			//		resultData.PutExtra(ExtraSaveToAlbum, this.saveToAlbum);
			//		SetResult(Result.Ok, resultData);
			//	}

			//	Finish();
			//}
		}

		private static Task<T> TaskFromResult<T>(T result)
		{
			var tcs = new TaskCompletionSource<T>();
			tcs.SetResult(result);
			return tcs.Task;
		}

		private void DeleteOutputFile()
		{
			try
			{
				if (this.path?.Scheme != "file")
					return;

				var localPath = GetLocalPath(this.path);

				if (File.Exists(localPath))
				{
					File.Delete(localPath);
				}
			}
			catch (Exception ex)
			{
				System.Diagnostics.Debug.WriteLine("Unable to delete file: " + ex.Message);
			}
		}

		private static int GetVideoQuality(VideoQuality videoQuality)
		{
			switch (videoQuality)
			{
				case VideoQuality.Medium:
				case VideoQuality.High:
					return 1;

				default:
					return 0;
			}
		}

		private static void OnMediaPicked(MediaPickedEventArgs e)
		{
			var picked = MediaPicked;
			if (picked != null)
				picked(null, e);
		}

		private void Touch()
		{
			if (this.path.Scheme != "file")
				return;

			File.Create(GetLocalPath(this.path)).Close();
		}

		private static string GetLocalPath(Uri uri)
		{
			return new System.Uri(uri.ToString()).LocalPath;
		}

		public static Uri GetOutputMediaFile(Context context, string subdir, string name, bool isPhoto, bool saveToAlbum)
		{
			subdir = subdir ?? String.Empty;

			if (String.IsNullOrWhiteSpace(name))
			{
				string timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
				if (isPhoto)
					name = "IMG_" + timestamp + ".jpg";
				else
					name = "VID_" + timestamp + ".mp4";
			}

			string mediaType = (isPhoto) ? Environment.DirectoryPictures : Environment.DirectoryMovies;
			var directory = saveToAlbum ? Environment.GetExternalStoragePublicDirectory(mediaType) : context.GetExternalFilesDir(mediaType);
			using (Java.IO.File mediaStorageDir = new Java.IO.File(directory, subdir))
			{
				if (!mediaStorageDir.Exists())
				{
					if (!mediaStorageDir.Mkdirs())
						throw new IOException("Couldn't create directory, have you added the WRITE_EXTERNAL_STORAGE permission?");

					if (!saveToAlbum)
					{
						// Ensure this media doesn't show up in gallery apps
						using (Java.IO.File nomedia = new Java.IO.File(mediaStorageDir, ".nomedia"))
							nomedia.CreateNewFile();
					}
				}

				return Uri.FromFile(new Java.IO.File(GetUniquePath(mediaStorageDir.Path, name, isPhoto)));
			}
		}

		private static string GetUniquePath(string folder, string name, bool isPhoto)
		{
			string ext = Path.GetExtension(name);
			if (ext == String.Empty)
				ext = ((isPhoto) ? ".jpg" : ".mp4");

			name = Path.GetFileNameWithoutExtension(name);

			string nname = name + ext;
			int i = 1;
			while (File.Exists(Path.Combine(folder, nname)))
				nname = name + "_" + (i++) + ext;

			return Path.Combine(folder, nname);
		}

	}
}
