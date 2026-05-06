// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Extensions.DependencyInjection;
using TestApps.Maui.Services;

namespace TestApps.Maui;

/// <summary>
/// Root application class. Starts the Arc backend before the window is created.
/// </summary>
public partial class App : Application
{
    readonly IServiceProvider _serviceProvider;

    /// <summary>
    /// Initializes a new instance of the <see cref="App"/> class.
    /// </summary>
    /// <param name="serviceProvider">The DI service provider.</param>
    public App(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
        InitializeComponent();
    }

    /// <inheritdoc/>
    protected override Window CreateWindow(IActivationState? activationState)
    {
        // Start the Arc HTTP backend synchronously so it is ready before the WebView loads.
        var arcHost = _serviceProvider.GetRequiredService<ArcHostService>();
        arcHost.StartAsync(FileSystem.AppDataDirectory).GetAwaiter().GetResult();

        var mainPage = _serviceProvider.GetRequiredService<MainPage>();

        return new Window(mainPage);
    }
}
