using Dynamsoft.BarcodeReaderBundle.Maui;

namespace ScanBarcodes_ReadyToUseComponent;
public partial class MainPage : ContentPage
{
	public MainPage()
	{
		InitializeComponent();
	}

	private async void OnScanSingleBarcode(object sender, EventArgs e)
    {
		// Initialize the license.
        // The license string here is a trial license. Note that network connection is required for this license to work.
        // You can request an extension via the following link: https://www.dynamsoft.com/customer/license/trialLicense?product=dbr&utm_source=samples&package=mobile
        var config = new BarcodeScannerConfig("DLS2eyJvcmdhbml6YXRpb25JRCI6IjIwMDAwMSJ9");
        var result = await BarcodeScanner.Start(config);
        var message = result.ResultStatus switch
        {
            EnumResultStatus.Finished => string.Join("\n",
                result.Barcodes!.Select(item => "Format: "+item.FormatString + "\nText: " + item.Text)),
            EnumResultStatus.Canceled => "Scanning canceled",
            EnumResultStatus.Exception => result.ErrorString,
            _ => throw new ArgumentOutOfRangeException()
        };
		label.Text = message;
    }
	
	private async void OnScanMultipleBarcode(object sender, EventArgs e)
    {
		// Initialize the license.
        // The license string here is a trial license. Note that network connection is required for this license to work.
        // You can request an extension via the following link: https://www.dynamsoft.com/customer/license/trialLicense?product=dbr&utm_source=samples&package=mobile
        var config = new BarcodeScannerConfig("DLS2eyJvcmdhbml6YXRpb25JRCI6IjIwMDAwMSJ9");
        config.ScanningMode = EnumScanningMode.Multiple;
        var result = await BarcodeScanner.Start(config);
        var message = result.ResultStatus switch
        {
            EnumResultStatus.Finished =>
	            "Barcodes Count: "+result.Barcodes?.Length + "\n\n" +
	            string.Join("\n\n", result.Barcodes!.Select(item => "Format: "+item.FormatString + "\nText:" + item.Text)),
            EnumResultStatus.Canceled => "Scanning canceled",
            EnumResultStatus.Exception => result.ErrorString,
            _ => throw new ArgumentOutOfRangeException()
        };
		label.Text = message;
    }
}

