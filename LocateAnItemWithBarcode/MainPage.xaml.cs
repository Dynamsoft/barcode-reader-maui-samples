namespace LocateAnItemWithBarcode;

public partial class MainPage : ContentPage
{
	public MainPage()
	{
		InitializeComponent();
	}

	private async void OnScanPressed(object? sender, EventArgs e)
	{
		ScanResultDelegateBridge.SetScanResultDelegate((string text) =>
		{
			MainThread.BeginInvokeOnMainThread(() =>
			{
				ItemIdEntry.Text = text;
			});
		});

		await Navigation.PushAsync(new CameraPage(CameraMode.Scan));
	}

	private async void OnStartSearchPressed(object? sender, EventArgs e)
	{
		string itemId = ItemIdEntry.Text?.Trim() ?? string.Empty;
		if (string.IsNullOrEmpty(itemId))
		{
			await DisplayAlertAsync("Hint", "Please enter or scan an Item ID first.", "OK");
			return;
		}

		await Navigation.PushAsync(new CameraPage(CameraMode.Search, itemId));
	}

	void OnItemIdCompleted(object? sender, EventArgs e)
	{
		ItemIdEntry.Unfocus();
	}
}
