namespace ScanBarcodes_FoundationalAPI;

public partial class MainPage : ContentPage
{

	public MainPage()
	{
		InitializeComponent();
	}

	private async void OnScanBarcode(object sender, EventArgs e)
    {
        await Navigation.PushAsync(new CameraPage());
    }
}

