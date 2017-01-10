using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.ProjectOxford.Vision;
using UIKit;
using Microsoft.ProjectOxford.Vision.Contract;
using Foundation;
using GPUImage.Sources;
using GPUImage.Filters.Effects;
using GPUImage.Outputs;
using CoreGraphics;
using AVFoundation;
using AudioToolbox;

namespace GitCommitDemo.iOS
{
	public partial class ViewController : UIViewController
	{
		int count = 1;
		// pushed on git
		// local branch commited.....

		GPUImageMovie movieFile;
		GPUImageVignetteFilter filter;
		GPUImageMovieWriter movieWriter;
		//GPUImageView imageView;

		public ViewController (IntPtr handle) : base (handle)
		{
		}

		public override void ViewDidLoad ()
		{
			base.ViewDidLoad ();

			// Perform any additional setup after loading the view, typically from a nib.
			Button.AccessibilityIdentifier = "myButton";

			Button.TouchUpInside += delegate {
				//var title = string.Format ("{0} clicks!", count++);
				//Button.SetTitle (title, UIControlState.Normal);

				var imagePicker = new UIImagePickerController ();
				imagePicker.MediaTypes = UIImagePickerController.AvailableMediaTypes (UIImagePickerControllerSourceType.PhotoLibrary);
				imagePicker.SourceType = UIImagePickerControllerSourceType.PhotoLibrary;
				PresentViewController (imagePicker, true, null);
				imagePicker.Canceled += async delegate {
					await imagePicker.DismissViewControllerAsync (true);
				};

				imagePicker.FinishedPickingMedia += (object s, UIImagePickerMediaPickedEventArgs e) => {
					//Insert code here for upload to Cognitive Services

					switch (e.Info [UIImagePickerController.MediaType].ToString ()) {

					case "public.movie":

						//moviePlayer = new MPMoviePlayerController (NSUrl.FromFilename ("out.mov"));
						//View.AddSubview (moviePlayer.View);
						//moviePlayer.SetFullscreen (true, true);
						//moviePlayer.Play ();
						//if (imageView == null) {
						//	imageView = new GPUImageView {
						//		Frame = View.Frame
						//	};
						//	View.AddSubview (imageView);
						//}

						AVPlayer mainPlayer = new AVPlayer ();
						AVPlayerItem playerItem = AVPlayerItem.FromUrl (e.MediaUrl);
						mainPlayer.ReplaceCurrentItemWithPlayerItem (playerItem);

						var shared = AVAudioSession.SharedInstance ();
						shared.SetCategory (AVAudioSessionCategory.Playback);
						AudioSession.OverrideCategoryMixWithOthers = true;
						AudioSession.SetActive (true);

						movieFile = new GPUImageMovie (playerItem.Asset);
						movieFile.RunBenchmark = true;
						movieFile.PlayAtActualSpeed = false;
						movieFile.PlayerItem = playerItem;

						filter = new GPUImageVignetteFilter ();
						filter.ForceProcessingAtSize (new CGSize (640.0f, 480.0f));
						filter.VignetteCenter = gpuImageView.Center;
						filter.VignetteStart = gpuImageView.Frame.Left;
						filter.VignetteEnd = gpuImageView.Frame.Right;
						movieFile.AddTarget (filter);
						filter.AddTarget (gpuImageView);

						var documents = Environment.GetFolderPath (Environment.SpecialFolder.MyDocuments);
						var pathToMovie = Path.Combine (documents, "Movie.m4v");
						if (File.Exists (pathToMovie)) {
							File.Delete (pathToMovie);
						}

						var movieURL = new NSUrl (pathToMovie, false);
						var asset = AVAsset.FromUrl (movieURL);

						AVAssetExportSession av = new AVAssetExportSession (asset, AVAssetExportSession.PresetMediumQuality);
						av.OutputUrl = NSUrl.FromFilename (pathToMovie);
						//	if (av.Status != AVAssetWriterStatus.Writing) {
						movieWriter = new GPUImageMovieWriter (av.OutputUrl, new CGSize (640.0f, 480.0f));
						filter.AddTarget (movieWriter);
						movieWriter.Enabled = true;
						movieWriter.ShouldPassthroughAudio = true;
						movieFile.AudioEncodingTarget = movieWriter;
						Console.WriteLine (movieWriter.HasAudioTrack);
						movieFile.EnableSynchronizedEncoding (movieWriter);
						//	}

						var timer = NSTimer.CreateRepeatingScheduledTimer (0.3, _ => {
							label.Text = movieFile.Progress.ToString ("P0");
						});

						movieWriter.StartRecording ();
						movieFile.StartProcessing ();
						mainPlayer.Play ();

						movieWriter.CompletionHandler = async () => {
							filter.RemoveTarget (movieWriter);
							await movieWriter.FinishRecordingAsync ();
							InvokeOnMainThread (() => {
								timer.Invalidate ();
								label.Text = 1.ToString ("P0");
							});
						};

						break;

					case "public.image":
						//if (e.OriginalImage != null) {
						//	gpuImageView = e.OriginalImage;
						//	var image = ScaledImage (e.OriginalImage, 190, 190);
						//	var details = await GetImageDescription (image.AsPNG ().AsStream ());
						//	if (details.Adult != null && details.Faces != null && details.Faces.Length > 0)
						//		label.Text = details.Description.Captions [0].Text + " gender " + details.Faces [0].Gender + " age " + details.Faces [0].Age + " Tags: " + details.Tags [0].Name + " adult " + details.Adult.IsAdultContent + " score: " + details.Adult.AdultScore;
						//	else if (details.Adult != null)
						//		label.Text = details.Description.Captions [0].Text + " adult " + details.Adult.IsAdultContent + " score: " + details.Adult.AdultScore;
						//	else
						//		label.Text = details.Description.Captions [0].Text;
						//}
						break;
					}

					DismissViewController (true, null);
				};
			};
		}

		UIImage ScaledImage (UIImage image, nfloat maxWidth, nfloat maxHeight)
		{
			var maxResizeFactor = Math.Min (maxWidth / image.Size.Width, maxHeight / image.Size.Height);
			var width = maxResizeFactor * image.Size.Width;
			var height = maxResizeFactor * image.Size.Height;
			return image.Scale (new CGSize (width, height));
		}

		public async Task<AnalysisResult> GetImageDescription (Stream imageStream)
		{
			var visionClient = new VisionServiceClient ("4c9567c549774928abc9f7236d05d399");

			VisualFeature [] features = { VisualFeature.Tags, VisualFeature.Categories, VisualFeature.Description, VisualFeature.Faces, VisualFeature.Adult, VisualFeature.Color };
			var res = await visionClient.AnalyzeImageAsync (imageStream, features.ToList (), null);

			return res;
		}

		public override void DidReceiveMemoryWarning ()
		{
			base.DidReceiveMemoryWarning ();
			// Release any cached data, images, etc that aren't in use.		

			Console.WriteLine ("memory warning");
		}
	}
}
