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
	public static class Paths
	{
		public static string ProduceUniquePath(string folder, string name)
		{ 
			string ext = Path.GetExtension(name);
			if (ext == String.Empty) ext = ".jpg";

			name = Path.GetFileNameWithoutExtension(name);

			string next_name = name + ext;
			int i = 1;
			while (File.Exists(Path.Combine(folder, next_name)))
				next_name = name + "_" + (i++) + ext;

			return Path.Combine(folder, next_name);
		}

		public static string GetLocalPath(Uri uri)
		{
			return new System.Uri(uri.ToString()).LocalPath;
		}
	}

	[Activity(ConfigurationChanges = ConfigChanges.Orientation | ConfigChanges.ScreenSize)]
	[Preserve(AllMembers = true)]
	public class MediaPickerActivity: Activity
	{
		internal const string ExtraPath = "path";
		internal const string ExtraLocation = "location";
		internal const string ExtraType = "type";
		internal const string ExtraId = "id";
		internal const string ExtraSaveToAlbum = "album_save";
		internal const string ExtraFront = "android.intent.extras.CAMERA_FACING";
		internal const string ExtraRunning = "running";

        internal static event EventHandler<MediaPickedEventArgs> MediaPicked;

		private const string Action = MediaStore.ActionImageCapture;

		private int id;
		private string title;
		private string description;
		private string type;
		private bool saveToAlbum;
		private string extraPath;
		private Uri path;

		public void OnMediaPicked(MediaPickedEventArgs eventArgs)
		{
			if (eventArgs != null) 
				MediaPicked(this, eventArgs);
		}

		protected override void OnSaveInstanceState(Bundle outState)
		{
			outState.PutBoolean(ExtraRunning, true);
			outState.PutString(MediaStore.MediaColumns.Title, this.title);
			outState.PutString(MediaStore.Images.ImageColumns.Description, this.description);
			outState.PutInt(ExtraId, this.id);
			outState.PutString(ExtraType, this.type);
			outState.PutString(ExtraPath, this.extraPath);

			if (this.path != null)
				outState.PutString(ExtraPath, this.path.Path);

			base.OnSaveInstanceState(outState);
		}

		protected override void OnCreate(Bundle savedInstanceState)
		{
			base.OnCreate(savedInstanceState);

			Bundle b = (savedInstanceState ?? Intent.Extras);

			bool ran = b.GetBoolean(ExtraRunning, false);
			this.title = b.GetString(MediaStore.MediaColumns.Title, "IMG_" + DateTime.Now.ToString("yyyyMMdd_HHmmss") + ".jpg");
			this.description = b.GetString(MediaStore.Images.ImageColumns.Description);
			this.id = b.GetInt(ExtraId, 0);
			this.type = b.GetString(ExtraType);
			this.saveToAlbum = b.GetBoolean(ExtraSaveToAlbum);
			this.extraPath = b.GetString(ExtraPath, "");

			var pickIntent = new Intent(Action);
			pickIntent.PutExtra(ExtraSaveToAlbum, this.saveToAlbum);

			try
			{
				if (!ran)
				{
					var directory =
						this.saveToAlbum
							? Environment.GetExternalStoragePublicDirectory(Environment.DirectoryPictures)
							: Application.Context.GetExternalFilesDir(Environment.DirectoryPictures);

					using (Java.IO.File mediaStorageDir = new Java.IO.File(directory, this.extraPath))
					{
						if (!mediaStorageDir.Exists())
						{
							var result = mediaStorageDir.Mkdirs();
							if (!result)
								throw new IOException("Couldn't create directory, have you added the WRITE_EXTERNAL_STORAGE permission?");
						}

						this.path = Uri.FromFile(new Java.IO.File(Paths.ProduceUniquePath(mediaStorageDir.Path, this.title)));
					}


					if (this.path.Scheme == "file")
						File.Create(Paths.GetLocalPath(this.path)).Close();

					pickIntent.PutExtra(MediaStore.ExtraOutput, this.path.Path);
					StartActivityForResult(pickIntent, this.id);
				}
				else
				{
					this.path = Uri.Parse(this.extraPath);
				}
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

		protected override void OnActivityResult(int requestCode, Result resultCode, Intent data)
		{
			base.OnActivityResult(requestCode, resultCode, data);
			OnMediaPicked(new MediaPickedEventArgs(this.id, false, this.path.Path));
			Finish();
		}
	}
}
