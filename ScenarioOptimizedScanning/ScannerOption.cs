namespace ScenarioOptimizedScanning;

public sealed class ScannerOption(string label, string imageSource, string templateFile, Color backgroundColor)
{
	public string Label { get; } = label;
	public string ImageSource { get; } = imageSource;
	public string TemplateFile { get; } = templateFile;
	public Color BackgroundColor { get; } = backgroundColor;
}