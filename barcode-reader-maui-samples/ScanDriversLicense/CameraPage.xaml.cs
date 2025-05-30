using Dynamsoft.Core.Maui;
using Dynamsoft.CameraEnhancer.Maui;
using Dynamsoft.CaptureVisionRouter.Maui;
using Dynamsoft.License.Maui;
using Dynamsoft.CodeParser.Maui;

namespace ScanDriversLicense;

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
        router?.StartCapturing("ReadDriversLicense", this);
    }

    protected override void OnDisappearing()
    {
        base.OnDisappearing();
        enhancer?.Close();
        router?.StopCapturing();
    }

	public void OnParsedResultsReceived(ParsedResult result)
    {
        if (result?.Items?.Length > 0)
        {
            ParsedResultItem parsedResultItem = result.Items[0];
            var dictionary = ParsedItemToDriverLicenseDict(parsedResultItem);
            if (dictionary != null) {
                router?.StopCapturing();
                enhancer?.ClearBuffer();
                MainThread.BeginInvokeOnMainThread(async () =>
                {
                    await Navigation.PushAsync(new ResultPage(dictionary));
                });
            }
        }
    }

	public static Dictionary<string, string>? ParsedItemToDriverLicenseDict(ParsedResultItem item)
	{
		if (item?.ParsedFields == null)
			return null;

		var fields = item.ParsedFields;

		string GetFieldValue(string key)
			=> fields.TryGetValue(key, out var field) ? field?.Value : null;

		var dict = new Dictionary<string, string>
		{
			["DocumentType"] = item.CodeType
		};

		if (item.CodeType == "AAMVA_DL_ID")
		{
			var name = GetFieldValue("fullName") ??
					$"{GetFieldValue("givenName") ?? GetFieldValue("firstName") ?? ""} {GetFieldValue("lastName") ?? ""}".Trim();

			dict["Name"] = name;
			dict["City"] = GetFieldValue("city");
			dict["State"] = GetFieldValue("jurisdictionCode");
			dict["Address"] = $"{GetFieldValue("street_1") ?? ""} {GetFieldValue("street_2") ?? ""}".Trim();
			dict["LicenseNumber"] = GetFieldValue("licenseNumber");
			dict["IssuedDate"] = GetFieldValue("issuedDate");
			dict["ExpirationDate"] = GetFieldValue("expirationDate");
			dict["BirthDate"] = GetFieldValue("birthDate");
			dict["Height"] = GetFieldValue("height");
			dict["Sex"] = GetFieldValue("sex");
			dict["IssuedCountry"] = GetFieldValue("issuingCountry");
			dict["VehicleClass"] = GetFieldValue("vehicleClass");
		}
		else if (item.CodeType == "AAMVA_DL_ID_WITH_MAG_STRIPE")
		{
			dict["Name"] = GetFieldValue("name");
			dict["City"] = GetFieldValue("city");
			dict["StateOrProvince"] = GetFieldValue("stateOrProvince");
			dict["Address"] = GetFieldValue("address");
			dict["LicenseNumber"] = GetFieldValue("DLorID_Number");
			dict["ExpirationDate"] = GetFieldValue("expirationDate");
			dict["BirthDate"] = GetFieldValue("birthDate");
			dict["Height"] = GetFieldValue("height");
			dict["Sex"] = GetFieldValue("sex");
		}
		else if (item.CodeType == "SOUTH_AFRICA_DL")
		{
			dict["Name"] = GetFieldValue("surname");
			dict["IdNumber"] = GetFieldValue("idNumber");
			dict["IdNumberType"] = GetFieldValue("idNumberType");
			dict["LicenseNumber"] = GetFieldValue("idNumber") ?? GetFieldValue("licenseNumber");
			dict["LicenseIssueNumber"] = GetFieldValue("licenseIssueNumber");
			dict["Initials"] = GetFieldValue("initials");
			dict["IssuedDate"] = GetFieldValue("licenseValidityFrom");
			dict["ExpirationDate"] = GetFieldValue("licenseValidityTo");
			dict["BirthDate"] = GetFieldValue("birthDate");
			dict["Sex"] = GetFieldValue("gender");
			dict["IssuedCountry"] = GetFieldValue("idIssuedCountry");
			dict["DriverRestrictionCodes"] = GetFieldValue("driverRestrictionCodes");
		}

		// Check essential fields
		if (!dict.TryGetValue("Name", out var nameVal) || string.IsNullOrWhiteSpace(nameVal) ||
			!dict.TryGetValue("LicenseNumber", out var licenseVal) || string.IsNullOrWhiteSpace(licenseVal))
		{
			return null;
		}

		return dict;
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