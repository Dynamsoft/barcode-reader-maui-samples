using System.Text;
using Dynamsoft.BarcodeReader.Maui;
using Dynamsoft.CameraEnhancer.Maui;
using Dynamsoft.CaptureVisionRouter.Maui;
using Dynamsoft.Core.Maui;
using Dynamsoft.License.Maui;
using Dynamsoft.Utility.Maui;

namespace BarcodeReaderSettings;

public partial class MainPage : ContentPage, ICapturedResultReceiver, ICompletionListener, ILicenseVerificationListener
{
	private readonly CameraEnhancer enhancer;
	private readonly CaptureVisionRouter router;
	public MainPage()
	{
		InitializeComponent();

		// Initialize the license.
        // The license string here is a trial license. Note that network connection is required for this license to work.
        // You can request an extension via the following link: https://www.dynamsoft.com/customer/license/trialLicense?product=dbr&utm_source=samples&package=mobile
		LicenseManager.InitLicense("DLS2eyJvcmdhbml6YXRpb25JRCI6IjIwMDAwMSJ9", this);

		enhancer = new CameraEnhancer();
		router = new CaptureVisionRouter();
		router.SetInput(enhancer);
		router.AddResultReceiver(this);

		UpdateResultView(0, string.Empty);
		// update settings before starting scanning if needed.
		// UpdateSettings();
	}

	protected override void OnHandlerChanged()
	{
		base.OnHandlerChanged();
		if (Handler != null)
		{
			enhancer.SetCameraView(camera);
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
		StopScanning();
	}

	private void StartScanning()
	{
		try
		{
			enhancer.Open();
			router.StartCapturing(EnumPresetTemplate.PT_READ_BARCODES, this);
		}
		catch (Exception ex)
		{
			MainThread.BeginInvokeOnMainThread(async () => await DisplayAlertAsync("Start Error", ex.Message, "OK"));
		}
	}

	private void StopScanning()
	{

		router.StopCapturing();
		enhancer.Close();
	}

	public void OnDecodedBarcodesReceived(DecodedBarcodesResult result)
	{
		if (result?.Items == null || result.Items.Length == 0)
		{
			return;
		}

		var builder = new StringBuilder();
		for (var i = 0; i < result.Items.Length; i++)
		{
			var item = result.Items[i];
			if (i > 0)
			{
				builder.AppendLine();
				builder.AppendLine();
			}

			builder.Append(i + 1);
			builder.Append(". Format: ");
			builder.Append(item.FormatString);
			builder.AppendLine();
			builder.Append("    Text: ");
			builder.Append(item.Text);
		}

		MainThread.BeginInvokeOnMainThread(() =>
		{
			UpdateResultView(result.Items.Length, builder.ToString());
		});
	}

	public void OnLicenseVerified(bool isSuccess, string? message)
	{
		if (!isSuccess)
		{
			Console.WriteLine($"License initialization failed: {message}");
		}
	}

	public void OnFailure(int errorCode, string errorMessage)
	{
		MainThread.BeginInvokeOnMainThread(async () =>
		{
			await DisplayAlertAsync("Error", errorMessage, "OK");
		});
	}

	private void UpdateResultView(int count, string text)
	{
		ResultCountLabel.Text = $"Total Results: {count}";
		ResultTextLabel.Text = string.IsNullOrWhiteSpace(text) ? "Waiting for barcode..." : text;
	}

	private void UpdateSettings()
	{
		try
		{
			var settings = router.GetSimplifiedSettings(EnumPresetTemplate.PT_READ_BARCODES);
			if (settings?.BarcodeSettings != null)
			{
				// Modify the settings as needed

				// barcode format
				settings.BarcodeSettings?.BarcodeFormatIds = EnumBarcodeFormat.BF_ALL;

				// expected barcodes count
				settings.BarcodeSettings?.ExpectedBarcodesCount = 0;

				// minimum result confidence
				settings.BarcodeSettings?.MinResultConfidence = 30;

				// scale down threshold
				settings.BarcodeSettings?.ScaleDownThreshold = 1200;

				// minimum barcode text length
				settings.BarcodeSettings?.MinBarcodeTextLength = 0;

				// timeout
				settings.Timeout = 500;

				// minimum image capture interval
				settings.MinImageCaptureInterval = 100;

				router.UpdateSettings(EnumPresetTemplate.PT_READ_BARCODES, settings);
			}
		}
		catch (Exception ex)
		{
			MainThread.BeginInvokeOnMainThread(async () => await DisplayAlertAsync("Update settings Error", ex.Message, "OK"));
		}
		}
	}
