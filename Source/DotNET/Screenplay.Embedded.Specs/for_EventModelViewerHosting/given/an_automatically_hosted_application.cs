// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reflection;
using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Cratis.Arc.Screenplay.Embedded.for_EventModelViewerHosting.given;

/// <summary>
/// A real host that activates the explorer the way the Cratis meta-package does, rather than mapping it itself.
/// </summary>
/// <remarks>
/// The specifications state the exposure explicitly: whether the test runner's own entry assembly was optimized
/// is not something a specification should depend on, and the decision made from that declaration is specified
/// on its own in <c>when_deciding_exposure</c>.
/// </remarks>
public abstract class an_automatically_hosted_application : Specification
{
    protected WebApplication _app;
    protected HttpClient _client;

    protected virtual bool? Exposure => true;

    protected virtual string EnvironmentName => Environments.Production;

    protected virtual bool RequiresAuthorization => false;

    protected virtual bool MapsTheExplorerExplicitly => false;

    protected virtual IReadOnlyList<Assembly> ExplicitlyMappedAssemblies => [];

    protected virtual IReadOnlyList<Assembly> ConfiguredAssemblies => [typeof(Company.Library.Program).Assembly];

    async Task Establish()
    {
        var builder = WebApplication.CreateBuilder(new WebApplicationOptions { EnvironmentName = EnvironmentName });
        builder.WebHost.UseUrls("http://127.0.0.1:0");
        builder.Logging.ClearProviders();
        if (RequiresAuthorization)
        {
            builder.Services
                .AddAuthentication(AnonymousScheme)
                .AddScheme<AuthenticationSchemeOptions, AnonymousHandler>(AnonymousScheme, _ => { });
            builder.Services.AddAuthorization();
        }

        builder.Services.AddCratisEventModelViewer(options =>
        {
            options.Enabled = Exposure;
            options.RequireAuthorization = RequiresAuthorization;
            foreach (var assembly in ConfiguredAssemblies)
            {
                options.Assemblies.Add(assembly);
            }
        });

        _app = builder.Build();

        if (MapsTheExplorerExplicitly)
        {
            _app.MapCratisEventModel([.. ExplicitlyMappedAssemblies]);
        }

        _app.UseCratisEventModelViewer();

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

    protected Uri Url(string path) => new($"{EventModelEndpoints.Prefix}{path}", UriKind.Relative);

    protected int EndpointsForTheExplorer() => ((IEndpointRouteBuilder)_app).DataSources
        .SelectMany(source => source.Endpoints)
        .OfType<RouteEndpoint>()
        .Count(endpoint => $"/{endpoint.RoutePattern.RawText?.TrimStart('/')}".StartsWith(EventModelEndpoints.Prefix, StringComparison.Ordinal));

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
