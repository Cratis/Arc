// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reflection;
using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Cratis.Arc.Screenplay.Embedded.for_EventModelEndpoints.given;

/// <summary>
/// A real host, on a loopback port the operating system picks, serving whatever the specification maps.
/// </summary>
/// <remarks>
/// The explorer is routing, redirects, content types and authorization as much as it is mapping, and none of
/// that is exercised by calling a handler directly. Kestrel on an ephemeral port gives the specifications a
/// real request pipeline without taking a dependency on a test host package.
/// </remarks>
public abstract class a_host : Specification
{
    protected WebApplication _app;
    protected HttpClient _client;

    protected virtual string? PathBase => null;

    protected virtual bool RequiresAuthorization => false;

    protected virtual bool MapsTheExplorer => true;

    protected virtual IReadOnlyList<Assembly> Assemblies => [an_embedded_application.Assembly];

    async Task Establish()
    {
        var builder = WebApplication.CreateBuilder();
        builder.WebHost.UseUrls("http://127.0.0.1:0");
        builder.Logging.ClearProviders();
        if (RequiresAuthorization)
        {
            builder.Services
                .AddAuthentication(AnonymousScheme)
                .AddScheme<AuthenticationSchemeOptions, AnonymousHandler>(AnonymousScheme, _ => { });
            builder.Services.AddAuthorization();
        }

        _app = builder.Build();
        if (PathBase is not null)
        {
            _app.UsePathBase(PathBase);
        }

        if (MapsTheExplorer)
        {
            var group = _app.MapCratisEventModel([.. Assemblies]);
            if (RequiresAuthorization)
            {
                group.RequireAuthorization();
            }
        }

        await _app.StartAsync();

        _client = new HttpClient(new HttpClientHandler { AllowAutoRedirect = false, CheckCertificateRevocationList = true })
        {
            BaseAddress = new Uri(_app.Urls.First(), UriKind.Absolute)
        };
    }

    void Destroy()
    {
        _client?.Dispose();
        _app?.StopAsync().GetAwaiter().GetResult();
        _app?.DisposeAsync().GetAwaiter().GetResult();
    }

    protected Uri Url(string path) => new($"{PathBase}{EventModelEndpoints.Prefix}{path}", UriKind.Relative);

    const string AnonymousScheme = "Anonymous";

    /// <summary>
    /// Authenticates nothing, so an endpoint requiring authorization answers with a challenge.
    /// </summary>
    /// <param name="options">The options monitor for the scheme.</param>
    /// <param name="logger">The logger factory.</param>
    /// <param name="encoder">The URL encoder.</param>
    sealed class AnonymousHandler(
        IOptionsMonitor<AuthenticationSchemeOptions> options,
        ILoggerFactory logger,
        UrlEncoder encoder) : AuthenticationHandler<AuthenticationSchemeOptions>(options, logger, encoder)
    {
        protected override Task<AuthenticateResult> HandleAuthenticateAsync() => Task.FromResult(AuthenticateResult.NoResult());
    }
}
