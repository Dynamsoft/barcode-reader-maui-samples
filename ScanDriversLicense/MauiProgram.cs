﻿using Microsoft.Extensions.Logging;
using Dynamsoft.CameraEnhancer.Maui;
using Dynamsoft.CameraEnhancer.Maui.Handlers;

namespace ScanDriversLicense;

public static class MauiProgram
{
	public static MauiApp CreateMauiApp()
	{
		var builder = MauiApp.CreateBuilder();
		builder
			.UseMauiApp<App>()
			.ConfigureFonts(fonts =>
			{
				fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
				fonts.AddFont("OpenSans-Semibold.ttf", "OpenSansSemibold");
			})
			.ConfigureMauiHandlers(handlers =>
            {
                handlers.AddHandler<CameraView, CameraViewHandler>();
            });

#if DEBUG
		builder.Logging.AddDebug();
#endif

		return builder.Build();
	}
}
