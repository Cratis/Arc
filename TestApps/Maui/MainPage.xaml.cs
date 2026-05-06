// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace TestApps.Maui;

/// <summary>
/// The main page — a full-screen WebView pointing at the Arc HTTP backend.
/// </summary>
public partial class MainPage : ContentPage
{
    /// <summary>
    /// Initializes a new instance of the <see cref="MainPage"/> class.
    /// </summary>
    public MainPage()
    {
        InitializeComponent();
    }

    /// <inheritdoc/>
    protected override void OnAppearing()
    {
        base.OnAppearing();
        _webView.Source = new UrlWebViewSource { Url = "http://localhost:5001" };
    }
}
