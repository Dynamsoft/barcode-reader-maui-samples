namespace ScenarioOptimizedScanning;

public partial class MainPage : ContentPage
{
	public IReadOnlyList<ScannerOption> BarcodeFormats { get; } =
	[
		new("Any Codes", "common_2d_1d.png", string.Empty, Colors.Black),
		new("1D Retail", "oned_retail.png", "ReadOneDRetail.json", Color.FromArgb("#1D1D1D")),
		new("1D Industrial", "oned_industrial.png", "ReadOneDIndustrial.json", Colors.Black),
		new("QR Code", "qr_code.png", "ReadQR.json", Color.FromArgb("#1D1D1D")),
		new("Data Matrix", "data_matrix.png", "ReadDataMatrix.json", Colors.Black),
		new("Common 2D\ncodes", "common_2d.png", "ReadCommon2D.json", Color.FromArgb("#1D1D1D")),
		new("Aztec Code", "aztec_code.png", "ReadAztec.json", Colors.Black),
		new("Dot Code", "dot_code.png", "ReadDotCode.json", Color.FromArgb("#1D1D1D")),
		new("Direct Part\nMarking(DPM)", "dpm.png", "ReadDPM.json", Colors.Black),
		new("PDF417", "pdf417_code.png", "ReadPDF417.json", Color.FromArgb("#1D1D1D"))
	];

	public IReadOnlyList<ScannerOption> Scenarios { get; } =
	[
		new("High-Density\nCode", "high_density.png", "ReadDenseBarcodes.json", Colors.Black)
	];

	public MainPage()
	{
		InitializeComponent();
		BindingContext = this;
	}

	private async void OnOptionTapped(object? sender, TappedEventArgs e)
	{
		if (e.Parameter is not ScannerOption option)
		{
			return;
		}

		await Navigation.PushAsync(new ScannerPage(option));
	}
}
