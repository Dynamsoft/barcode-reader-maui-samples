using System.Text;
using Dynamsoft.BarcodeReader.Maui;
using Dynamsoft.CameraEnhancer.Maui;
using Dynamsoft.CaptureVisionRouter.Maui;
using Dynamsoft.Core.Maui;
using Dynamsoft.License.Maui;

namespace ScenarioOptimizedScanning;

public partial class ScannerPage : ContentPage, ICapturedResultReceiver, ICompletionListener, ILicenseVerificationListener
{
	private readonly ScannerOption option;
	private readonly CameraEnhancer camera;
	private readonly CaptureVisionRouter router;

	public ScannerPage(ScannerOption option)
	{
		InitializeComponent();
		this.option = option;
		Title = GetPageTitle(option.Label);

		// Initialize the license.
        // The license string here is a trial license. Note that network connection is required for this license to work.
        // You can request an extension via the following link: https://www.dynamsoft.com/customer/license/trialLicense?product=dbr&utm_source=samples&package=mobile
		LicenseManager.InitLicense("DLS2eyJvcmdhbml6YXRpb25JRCI6IjIwMDAwMSJ9", this);
		camera = new CameraEnhancer();
		router = new CaptureVisionRouter();
		router.SetInput(camera);
		router.AddResultReceiver(this);
		ConfigureScanner();
	}

	protected override void OnHandlerChanged()
	{
		base.OnHandlerChanged();
		if (Handler != null)
		{
			camera.SetCameraView(CameraPreview);
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
		router.StopCapturing();
		camera.Close();
	}

	private void ConfigureScanner()
	{
		try
		{
			if (!string.IsNullOrEmpty(option.TemplateFile))
			{
				router.InitSettingsFromFile(option.TemplateFile);
			}

			if (option.TemplateFile == "ReadDotCode.json")
			{
				camera.SetZoomFactor(3.0f);
				var rect = new DMRect(0.15f, 0.35f, 0.85f, 0.48f, true);
				camera.SetScanRegion(rect);
			}

			if (option.TemplateFile == "ReadPDF417.json")
			{
				camera.SetResolution(EnumResolution.RESOLUTION_4K);
			}
		}
		catch (Exception ex)
		{
			_ = ShowErrorAsync("Load template error", ex.Message);
		}
	}

	private void StartScanning()
	{
		try
		{
			camera.Open();
			var templateName = string.IsNullOrEmpty(option.TemplateFile) ? EnumPresetTemplate.PT_READ_BARCODES : string.Empty;
			router.StartCapturing(templateName, this);
		}
		catch (Exception ex)
		{
			_ = ShowErrorAsync("Start error", ex.Message);
		}
	}

	public void OnDecodedBarcodesReceived(DecodedBarcodesResult result)
	{
		if (result?.Items == null || result.Items.Length == 0)
		{
			return;
		}

		var builder = new StringBuilder();
		for (var index = 0; index < result.Items.Length; index++)
		{
			var item = result.Items[index];
			if (index > 0)
			{
				builder.AppendLine();
				builder.AppendLine();
			}

			builder.Append(index + 1);
			builder.Append(". Format: ");
			builder.Append(item.FormatString);
			builder.AppendLine();
			builder.Append("    Text: ");
			builder.Append(item.Text);
		}

		MainThread.BeginInvokeOnMainThread(() =>
		{
			ResultCountLabel.Text = $"Total Results: {result.Items.Length}";
			ResultTextLabel.Text = builder.ToString();
		});
	}

	private Task ShowErrorAsync(string title, string message)
	{
		if (Window == null)
		{
			return Task.CompletedTask;
		}

		return MainThread.InvokeOnMainThreadAsync(() => DisplayAlertAsync(title, message, "OK"));
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
		_ = ShowErrorAsync("Error", errorMessage);
	}

	private static string GetPageTitle(string? label)
	{
		if (string.IsNullOrWhiteSpace(label))
		{
			return "Scanner";
		}

		return label.Replace("\n", " ").Trim();
	}
}