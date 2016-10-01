using System;
using System.Threading.Tasks;
using Xamarin.Forms;

namespace Camera
{
	public class ImageCaptureResult
	{
		public string Path { get; set; }
		public bool Success { get; set; }
	}

	public interface IImageCapture
	{
		Task<ImageCaptureResult> Capture();
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
				var result = await DependencyService.Get<IImageCapture>().Capture();
				label.Text = result.Path;
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
