namespace LocateAnItemWithBarcode;

public delegate void ScanResultDelegate(string text);

public static class ScanResultDelegateBridge
{
	static ScanResultDelegate? _scanResultDelegate;

	public static void SetScanResultDelegate(ScanResultDelegate? scanResultDelegate)
	{
		_scanResultDelegate = scanResultDelegate;
	}

	public static void NotifyScanResult(string text)
	{
		_scanResultDelegate?.Invoke(text);
	}
}
