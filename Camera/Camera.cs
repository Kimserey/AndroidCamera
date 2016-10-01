using System;
using System.Threading.Tasks;
using Xamarin.Forms;

namespace Camera
{
	public interface IMediaManager
	{
		Task<string> TakePicture();
	}

	/// <summary>
	/// Photo size enum.
	/// </summary>
	public enum PhotoSize
	{
		/// <summary>
		/// 25% of original
		/// </summary>
		Small,
		/// <summary>
		/// 50% of the original
		/// </summary>
		Medium,
		/// <summary>
		/// 75% of the original
		/// </summary>
		Large,
		/// <summary>
		/// Untouched
		/// </summary>
		Full
	}

	/// <summary>
	/// Camera device
	/// </summary>
	public enum CameraDevice
	{
		/// <summary>
		/// Back of device
		/// </summary>
		Rear,
		/// <summary>
		/// Front facing of device
		/// </summary>
		Front
	}

	public class StoreCameraMediaOptions
	{
		/// <summary>
		/// Directory name
		/// </summary>
		public string Directory
		{
			get;
			set;
		}

		/// <summary>
		/// File name
		/// </summary>
		public string Name
		{
			get;
			set;
		}

		/// <summary>
		/// Default camera
		/// </summary>
		public CameraDevice DefaultCamera
		{
			get;
			set;
		}

		/// <summary>
		/// Get or set for an OverlayViewProvider
		/// </summary>
		public Func<Object> OverlayViewProvider
		{
			get;
			set;
		}

		/// <summary>
		// Get or set if the image should be stored public
		/// </summary>
		public bool SaveToAlbum
		{
			get; set;
		}

		/// <summary>
		/// Gets or sets the size of the photo.
		/// </summary>
		/// <value>The size of the photo.</value>
		public PhotoSize PhotoSize { get; set; } = PhotoSize.Full;

		int quality = 100;
		/// <summary>
		/// The compression quality to use, 0 is the maximum compression (worse quality),
		/// and 100 minimum compression (best quality)
		/// Default is 100
		/// </summary>
		public int CompressionQuality
		{
			get { return quality; }
			set
			{
				if (value > 100)
					quality = 100;
				else if (value < 0)
					quality = 0;
				else
					quality = value;
			}
		}

	}

	public class App : Application
	{
		public App()
		{
			var label =
				new Label
				{
					HorizontalTextAlignment = TextAlignment.Center,
					Text = "Path?"
				};

			var button =
				new Button
				{
					Text = "Take photo"
				};

			button.Clicked += async (sender, e) =>
			{
				var pic = await DependencyService.Get<IMediaManager>().TakePicture();
				label.Text = pic;
			};

			var content = new ContentPage
			{
				Title = "Camera",
				Content = new StackLayout
				{
					VerticalOptions = LayoutOptions.Center,
					Children = {
						label,
						button
					}
				}
			};

			MainPage = new NavigationPage(content);
		}
	}
}
