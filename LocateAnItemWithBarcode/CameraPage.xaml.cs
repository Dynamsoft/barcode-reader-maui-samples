using Dynamsoft.BarcodeReader.Maui;
using Dynamsoft.CameraEnhancer.Maui;
using Dynamsoft.CaptureVisionRouter.Maui;
using Dynamsoft.Core.Maui;
using Dynamsoft.License.Maui;
using Dynamsoft.Utility.Maui;

namespace LocateAnItemWithBarcode;

public enum CameraMode
{
	Scan,
	Search
}

public partial class CameraPage : ContentPage, ICapturedResultReceiver, ICompletionListener, ILicenseVerificationListener
{
	const string License = "DLS2eyJvcmdhbml6YXRpb25JRCI6IjIwMDAwMSJ9";

	readonly CameraEnhancer _camera;
	readonly CaptureVisionRouter _router;
	readonly MultiFrameResultCrossFilter _filter;
	readonly CameraMode _mode;
	readonly string? _targetText;
	readonly OverlayDrawable _overlayDrawable = new();
	bool _hasReturned;

	public CameraPage(CameraMode mode, string? targetText = null)
	{
		InitializeComponent();

		_mode = mode;
		_targetText = targetText;

		OverlayCanvas.Drawable = _overlayDrawable;

		_camera = new CameraEnhancer();
		_router = new CaptureVisionRouter();
		_filter = new MultiFrameResultCrossFilter();

		LicenseManager.InitLicense(License, this);
        try
        {
            _router.SetInput(_camera);
            _filter.EnableLatestOverlapping(EnumCapturedResultItemType.Barcode, true);
            _filter.SetMaxOverlappingFrames(EnumCapturedResultItemType.Barcode, 10);
            _router.AddResultFilter(_filter);
            _router.AddResultReceiver(this);

            _router.InitSettingsFromFile("ReadMultipleBarcodes.json");
        }
        catch (CoreException ex)
        {
            MainThread.BeginInvokeOnMainThread(() =>
            {
                _ = DisplayAlertAsync("Error", ex.ErrorMessage, "OK");
            });
        }
		ConfigureModeUi();
	}

	protected override void OnHandlerChanged()
	{
		base.OnHandlerChanged();
		if (Handler != null)
		{
			_camera.SetCameraView(CameraView);
            CameraView.GetDrawingLayer(EnumDrawingLayerId.DLI_DBR).Visible = false;
		}
	}

	protected override async void OnAppearing()
	{
		base.OnAppearing();
		await Permissions.RequestAsync<Permissions.Camera>();
		StartScanning();
	}

	protected override void OnDisappearing()
	{
		base.OnDisappearing();
		_router.StopCapturing();
		_camera.Close();
	}

	void ConfigureModeUi()
	{
		bool isSearchMode = _mode == CameraMode.Search;
		LocateAnotherButton.IsVisible = isSearchMode;
		StatusContainer.IsVisible = isSearchMode;
		if (isSearchMode)
		{
			UpdateStatus(false);
		}
	}

	void StartScanning()
	{
		_camera.Open();

		if (_mode == CameraMode.Search)
		{
			_router.StartCapturing("ReadMultipleBarcodes", this);
			return;
		}

		_router.ResetSettings();
		_router.StartCapturing(EnumPresetTemplate.PT_READ_BARCODES, this);
	}

	public void OnDecodedBarcodesReceived(DecodedBarcodesResult result)
	{
		BarcodeResultItem[] items = result?.Items ?? [];

		if (_mode == CameraMode.Scan)
		{
			if (items.Length > 0 && !_hasReturned)
			{
				_hasReturned = true;
				string text = items[0].Text ?? string.Empty;

				MainThread.BeginInvokeOnMainThread(async () =>
				{
					ScanResultDelegateBridge.NotifyScanResult(text);
					await Navigation.PopAsync();
				});
			}

			return;
		}

		if (items.Length == 0)
		{
			MainThread.BeginInvokeOnMainThread(() =>
			{
				_overlayDrawable.Markers = [];
				OverlayCanvas.Invalidate();
				UpdateStatus(false);
			});
			return;
		}

		List<OverlayMarker> markers = [];
		bool found = false;

		foreach (BarcodeResultItem item in items)
		{
			var center = item.Location.CentrePoint;
			var viewPoint = _camera.ConvertPointToViewCoordinates(center);
			bool isMatch = item.Text == _targetText;
			if (isMatch)
			{
				found = true;
			}
			markers.Add(new OverlayMarker(viewPoint.X, viewPoint.Y, isMatch, isMatch ? item.Text : null));
		}

		MainThread.BeginInvokeOnMainThread(() =>
		{
			RenderOverlays(markers);
			UpdateStatus(found);
		});
	}

	void RenderOverlays(IEnumerable<OverlayMarker> markers)
	{
		_overlayDrawable.Markers = markers.ToList();
		OverlayCanvas.Invalidate();
	}

	void UpdateStatus(bool found)
	{
		StatusContainer.BackgroundColor = found ? Color.FromArgb("#E64CAF50") : Color.FromArgb("#B3000000");
		StatusLabel.Text = found
			? $"Item Found: \"{_targetText ?? string.Empty}\""
			: $"Searching for: \"{_targetText ?? string.Empty}\"";
	}

	async void OnLocateAnotherClicked(object? sender, EventArgs e)
	{
		await Navigation.PopAsync();
	}

	protected override void OnNavigatedFrom(NavigatedFromEventArgs args)
	{
		base.OnNavigatedFrom(args);
		if (_mode == CameraMode.Scan)
		{
			ScanResultDelegateBridge.SetScanResultDelegate(null);
		}
	}

	public void OnLicenseVerified(bool isSuccess, string? message)
	{
		if (!isSuccess)
		{
			Console.WriteLine("License initialization failed: " + message);
		}
	}

	public void OnFailure(int errorCode, string errorMessage)
	{
		MainThread.BeginInvokeOnMainThread(() =>
		{
			_ = DisplayAlertAsync("Error", errorMessage, "OK");
		});
	}

	public readonly record struct OverlayMarker(double X, double Y, bool IsMatch, string? BarcodeText);
}

/// <summary>
/// Draws overlay marker bubbles (and matched barcode text) onto a <see cref="GraphicsView"/>.
/// Replaces the previous View-based (Border/Label) overlay rendering with direct canvas drawing.
/// </summary>
public sealed class OverlayDrawable : IDrawable
{
	const float BubbleRadius = 15f;

	public IReadOnlyList<CameraPage.OverlayMarker> Markers { get; set; } = [];

	public void Draw(ICanvas canvas, RectF dirtyRect)
	{
		foreach (var marker in Markers)
		{
			Color color = marker.IsMatch ? Color.FromArgb("#4CAF50") : Color.FromArgb("#F44336");
			float x = (float)marker.X;
			float y = (float)marker.Y;

			// Draw the round bubble.
			canvas.FillColor = color;
			canvas.FillEllipse(x - BubbleRadius, y - BubbleRadius, BubbleRadius * 2, BubbleRadius * 2);

			// Draw the "+"/"X" symbol centered in the bubble.
			canvas.FontColor = Colors.White;
			canvas.FontSize = 16;
			canvas.Font = Microsoft.Maui.Graphics.Font.DefaultBold;
			canvas.DrawString(
				marker.IsMatch ? "+" : "X",
				x - BubbleRadius, y - BubbleRadius, BubbleRadius * 2, BubbleRadius * 2,
				HorizontalAlignment.Center, VerticalAlignment.Center);

			// Draw the matched barcode text label below the bubble.
			if (marker.IsMatch && !string.IsNullOrEmpty(marker.BarcodeText))
			{
				canvas.FontColor = color;
				canvas.FontSize = 12;
				canvas.Font = Microsoft.Maui.Graphics.Font.DefaultBold;
				canvas.DrawString(
					marker.BarcodeText,
					x - 80, y + 20, 160, 24,
					HorizontalAlignment.Center, VerticalAlignment.Center,
					TextFlow.ClipBounds);
			}
		}
	}
}

