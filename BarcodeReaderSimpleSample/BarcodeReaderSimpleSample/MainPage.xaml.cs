using Dynamsoft.License.Maui;
using System.Diagnostics;
namespace BarcodeReaderSimpleSample;

public partial class MainPage : ContentPage, ILicenseVerificationListener
{

    public MainPage()
    {
        InitializeComponent();

        // Initialize the license.
        // The license string here is a trial license. Note that network connection is required for this license to work.
        // You can request an extension via the following link: https://www.dynamsoft.com/customer/license/trialLicense?product=dbr&utm_source=samples&package=mobile
        LicenseManager.InitLicense("DLS2eyJvcmdhbml6YXRpb25JRCI6IjIwMDAwMSJ9", this);
    }

    private async void OnCameraClicked(object sender, EventArgs e)
    {
        await Navigation.PushAsync(new CameraPage());
    }

    public void OnLicenseVerified(bool isSuccess, string message)
    {
        if (!isSuccess)
        {
            MainThread.BeginInvokeOnMainThread(() =>
            {
                errorMessage.Text = "License initialization failed: " + message;
            });
        }

    }
}


