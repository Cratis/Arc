// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Extensions.Logging;
using TestApps.Maui.Services;

namespace TestApps.Maui;

/// <summary>
/// Entry point for the MAUI application.
/// </summary>
public static class MauiProgram
{
    /// <summary>
    /// Creates and configures the <see cref="MauiApp"/>.
    /// </summary>
    /// <returns>The configured <see cref="MauiApp"/>.</returns>
    public static MauiApp CreateMauiApp()
    {
        var builder = MauiApp.CreateBuilder();
        builder.UseMauiApp<App>();

#if DEBUG
        builder.Logging.AddDebug();
#endif

#if MACCATALYST
        Microsoft.Maui.Handlers.WebViewHandler.Mapper.AppendToMapping("MacCatalystWebView", (handler, _) =>
        {
            var webView = handler.PlatformView;

            // Desktop content mode: viewport width = window width in CSS pixels
            // instead of the default iPad-scale treatment (~768 px).
            webView.Configuration.DefaultWebpagePreferences.PreferredContentMode =
                WebKit.WKContentMode.Desktop;

            // Mac Catalyst applies a ~77% OS-level scale to all iPad-origin windows.
            // Compensate so content renders at 100% instead of ~75%.
            // Adjust if the result still looks off — 1.3 = 1/0.77.
            webView.PageZoom = 1.3f;

#if DEBUG
            // Enable Safari Web Inspector so you can attach from Safari > Develop.
            webView.Configuration.Preferences.SetValueForKey(
                Foundation.NSObject.FromObject(true),
                new Foundation.NSString("developerExtrasEnabled"));
#endif
        });
#endif

        builder.Services.AddSingleton<ArcHostService>();
        builder.Services.AddTransient<MainPage>();

        return builder.Build();
    }
}
