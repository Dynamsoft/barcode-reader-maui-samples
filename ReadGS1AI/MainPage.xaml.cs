using System.Text.RegularExpressions;
using Dynamsoft.CameraEnhancer.Maui;
using Dynamsoft.CameraEnhancer.Maui.Handlers;
using Dynamsoft.CaptureVisionRouter.Maui;
using Dynamsoft.CodeParser.Maui;
using Dynamsoft.Core.Maui;
using Dynamsoft.License.Maui;

namespace ReadGS1AI;

public partial class MainPage : ContentPage, ICapturedResultReceiver, ICompletionListener, ILicenseVerificationListener
{
	private const string TemplateName = "ReadGS1AI";
	private readonly CameraEnhancer camera;
	private readonly CaptureVisionRouter router;

	public MainPage()
	{
		InitializeComponent();

		// Initialize the license.
        // The license string here is a trial license. Note that network connection is required for this license to work.
        // You can request an extension via the following link: https://www.dynamsoft.com/customer/license/trialLicense?product=dbr&utm_source=samples&package=mobile
		LicenseManager.InitLicense("DLS2eyJvcmdhbml6YXRpb25JRCI6IjIwMDAwMSJ9", this);
		camera = new CameraEnhancer();
		router = new CaptureVisionRouter();
		router.SetInput(camera);
		router.AddResultReceiver(this);
		router.InitSettingsFromFile("ReadGS1AI.json");
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
		try
		{
			camera.Open();
			router.StartCapturing(TemplateName, this);
		}
		catch (Exception ex)
		{
			await ShowErrorAsync("Start error", ex.Message);
		}
	}

	protected override void OnDisappearing()
	{
		base.OnDisappearing();			
		router.StopCapturing();
		camera.Close();
		camera.ClearBuffer();
	}

	public void OnParsedResultsReceived(ParsedResult result)
	{
		var item = result?.Items?.Length > 0 ? result.Items[0] : null;
		if (item == null)
		{
			return;
		}
		router.StopCapturing();

		var message = BuildGs1Message(item);
		MainThread.BeginInvokeOnMainThread(async () =>
		{
			try
			{
				await DisplayAlertAsync("GS1 Result", message, "OK");
			}
			finally
			{
				router.StartCapturing(TemplateName, this);
			}
		});
	}

	private static string BuildGs1Message(ParsedResultItem item)
	{
		var contents = GetContentsFromParsedItem(item);
		return string.Join("\n", contents);
	}

	/// <summary>
	/// Extracts "AI description: data" strings from a parsed GS1 item, mirroring the
	/// Android sample logic (getContentsFromParsedItem).
	/// </summary>
	private static string[] GetContentsFromParsedItem(ParsedResultItem item)
	{
		var contents = new List<string>();
		var fields = item.ParsedFields;

		foreach (var fieldName in fields.Keys)
		{
			if (!IsAI(fieldName))
			{
				continue;
			}

			if (fields.TryGetValue(fieldName + "Data", out var dataField) &&
				dataField?.ValidationStatus == EnumValidationStatus.VS_FAILED)
			{
				continue;
			}

			var aiDescription = fields.TryGetValue(fieldName + "AI", out var aiField) ? aiField?.Value : null;
			var aiData = dataField?.Value;

			if (aiDescription != null && aiDescription.ToLowerInvariant().Contains("date"))
			{
				aiData = ParseDateAndTimeString(aiData);
			}

			if (aiData != null)
			{
				contents.Add($"{aiDescription}: {aiData}");
			}
		}

		return contents.ToArray();
	}

	private static bool IsAI(string? str)
	{
		return str != null && Regex.IsMatch(str, "^\\d{2,4}$");
	}

	/// <summary>
	/// Supported input formats:
	/// - yyMMdd (6 digits): Returns a date in the format "yy/MM/dd".
	/// - yyMMddHHmm (10 digits): Returns a date and time in the format "yy/MM/dd HH:mm".
	/// - yyMMddHHmmSS (12 digits): Returns a date and time in the format "yy/MM/dd HH:mm:ss".
	/// </summary>
	private static string? ParseDateAndTimeString(string? dateStr)
	{
		// Only support yyMMdd, yyMMddHHmm, yyMMddHHmmSS. All characters must be digits of appropriate length (6, 10, or 12).
		const string validPattern = "^(\\d{6}|\\d{10}|\\d{12})$";
		if (string.IsNullOrEmpty(dateStr) || !Regex.IsMatch(dateStr, validPattern))
		{
			return null;
		}

		var length = dateStr.Length;

		var year = int.Parse(dateStr.Substring(0, 2)) + 2000;
		var month = int.Parse(dateStr.Substring(2, 2));
		var day = int.Parse(dateStr.Substring(4, 2));

		// Handle day=0 -> last day of month
		var lastDay = DateTime.DaysInMonth(year, month);
		day = day == 0 ? lastDay : day;

		// Parse hour, minute, and second if the length allows
		var hour = 0;
		var minute = 0;
		var second = 0;
		if (length >= 10)
		{
			hour = int.Parse(dateStr.Substring(6, 2));
			minute = int.Parse(dateStr.Substring(8, 2));
			if (length == 12)
			{
				second = int.Parse(dateStr.Substring(10, 2));
			}
		}

		DateTime dateTime;
		try
		{
			dateTime = new DateTime(year, month, day, hour, minute, second);
		}
		catch (ArgumentOutOfRangeException)
		{
			return null;
		}

		var pattern = length switch
		{
			6 => "yy/MM/dd",
			10 => "yy/MM/dd HH:mm",
			_ => "yy/MM/dd HH:mm:ss",
		};

		return dateTime.ToString(pattern);
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
}
