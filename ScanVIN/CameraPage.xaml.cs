using Dynamsoft.Core.Maui;
using Dynamsoft.CaptureVisionRouter.Maui;
using Dynamsoft.CameraEnhancer.Maui;
using Dynamsoft.Utility.Maui;
using Dynamsoft.CodeParser.Maui;
using Dynamsoft.License.Maui;

namespace ScanVIN;

public partial class CameraPage : ContentPage, ICapturedResultReceiver, ICompletionListener, ILicenseVerificationListener
{
    readonly CameraEnhancer camera;
    readonly CaptureVisionRouter router;
	public CameraPage()
	{
		InitializeComponent();
		// Initialize the license.
        // The license string here is a trial license. Note that network connection is required for this license to work.
        // You can request an extension via the following link: https://www.dynamsoft.com/customer/license/trialLicense?product=cvs&utm_source=samples&package=mobile
        LicenseManager.InitLicense("DLS2eyJvcmdhbml6YXRpb25JRCI6IjIwMDAwMSJ9", this);
        camera = new CameraEnhancer();
        router = new CaptureVisionRouter();
        router.SetInput(camera);
        router.AddResultReceiver(this);
        var filter = new MultiFrameResultCrossFilter();
        filter.EnableResultCrossVerification(EnumCapturedResultItemType.Barcode, true);
        router.AddResultFilter(filter);
	}

	protected override void OnHandlerChanged()
    {
        base.OnHandlerChanged();
        if (Handler != null)
        {
            camera.SetCameraView(CameraView);
        }
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await Permissions.RequestAsync<Permissions.Camera>();
        camera?.Open();
        router?.StartCapturing("ReadVINBarcode", this);
    }

    protected override void OnDisappearing()
    {
        base.OnDisappearing();
        camera?.Close();
        router?.StopCapturing();
    }

    public void OnParsedResultsReceived(ParsedResult result)
    {
        if (result?.Items?.Length > 0)
        {
            ParsedResultItem parsedResultItem = result.Items[0];

            var dictionary = ConvertToVINDictionary(parsedResultItem);
            if (dictionary != null) {
                router?.StopCapturing();
                camera?.ClearBuffer();
                MainThread.BeginInvokeOnMainThread(async () =>
                {
                    await Navigation.PushAsync(new ResultPage(dictionary));
                });
            }
        }
    }

    public Dictionary<string, string>? ConvertToVINDictionary(ParsedResultItem item) 
    {
        if (item.ParsedFields.TryGetValue("vinString", out ParsedField? value) && value != null) {
            Dictionary<string, string> dic = [];
            string[] infoLists = ["vinString", "WMI", "region", "VDS", "checkDigit", "modelYear", "plantCode", "serialNumber"];
            foreach (var info in infoLists)
            {
                if(item.ParsedFields.TryGetValue(info, out ParsedField? field) && field != null) 
                {
                    if (item.ParsedFields[info].ValidationStatus == EnumValidationStatus.VS_FAILED) {
                        return null;
                    } else {
                        dic.Add(CapitalizeFirstLetter(info), item.ParsedFields[info].Value);
                    }
                }
            }
            return dic;
        } else {
            return null;
        }
    }

    public static string CapitalizeFirstLetter(string input)
    {
        if (string.IsNullOrWhiteSpace(input))
            return input;

        return char.ToUpper(input[0]) + input.Substring(1);
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