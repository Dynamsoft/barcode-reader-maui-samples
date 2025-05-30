using Dynamsoft.Core.Maui;
using Dynamsoft.BarcodeReader.Maui;
using Dynamsoft.CameraEnhancer.Maui;
using Dynamsoft.CaptureVisionRouter.Maui;
using Dynamsoft.Utility.Maui;
using Dynamsoft.License.Maui;

namespace ScanBarcodes_FoundationalAPI;

public partial class CameraPage : ContentPage, ICapturedResultReceiver, ICompletionListener, ILicenseVerificationListener
{
	CameraEnhancer enhancer;
    CaptureVisionRouter router;
	public CameraPage()
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
        var filter = new MultiFrameResultCrossFilter();
        filter.EnableResultCrossVerification(EnumCapturedResultItemType.Barcode, true);
        router.AddResultFilter(filter);
	}

	protected override void OnHandlerChanged()
    {
        base.OnHandlerChanged();
        if (this.Handler != null)
        {
            enhancer.SetCameraView(camera);
        }
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await Permissions.RequestAsync<Permissions.Camera>();
        enhancer?.Open();
        router?.StartCapturing(EnumPresetTemplate.PT_READ_SINGLE_BARCODE, this);
    }

    protected override void OnDisappearing()
    {
        base.OnDisappearing();
        enhancer?.Close();
        router?.StopCapturing();
    }

    public void OnDecodedBarcodesReceived(DecodedBarcodesResult result)
    {
        if (result != null && result.Items != null && result.Items.Length > 0)
        {
            MainThread.BeginInvokeOnMainThread(() =>
            {
                router?.StopCapturing();
                enhancer?.ClearBuffer();
            });
            var message = "";
            foreach (var item in result.Items)
            {
                message += "\nFormat: " + item.FormatString + "\nText: " + item.Text + "\n";
            }
            MainThread.BeginInvokeOnMainThread(async () =>
            {
                await DisplayAlert("Results", message, "OK");
                router?.StartCapturing(EnumPresetTemplate.PT_READ_SINGLE_BARCODE, this);
            });
        }
    }
	
	public void OnLicenseVerified(bool isSuccess, string message)
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
            DisplayAlert("Error", errorMessage, "OK");
        });
    }
}