// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Identity.Callers;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Cratis.Arc.Capabilities.for_CapabilitiesStartupValidation.given;

public class a_capability_startup_validation : Specification
{
    private protected ArcOptions _arc;
    protected CapabilitiesOptions _capabilities;
    private protected VerifiedCallerInstallation _installation;
    private protected CallerResourceCatalog _catalog;
    private protected ServiceCollection _services;
    protected Task _result;
    protected Exception? _error;
    ServiceProvider _provider;

    void Establish()
    {
        _arc = new ArcOptions();
        _capabilities = new CapabilitiesOptions();
        _arc.EndpointGroups.Assign("capabilities", 42001);
        _installation = new("https://resource.example", "test");
        _catalog = new(new Dictionary<string, string> { ["https://resource.example"] = "audience" });
        _services = new ServiceCollection();
        _services.AddScoped(_ => Substitute.For<IVerifiedCredentialSource>());
    }

    protected async Task Validate()
    {
        _provider = _services.BuildServiceProvider(new ServiceProviderOptions { ValidateScopes = true });
        var validation = new CapabilitiesStartupValidation(Options.Create(_arc), Options.Create(_capabilities), _installation, _catalog, _provider.GetRequiredService<IServiceScopeFactory>());
        _error = await Catch.Exception(async () =>
        {
            _result = validation.StartingAsync(CancellationToken.None);
            await _result;
        });
    }

    void Destroy() => _provider.Dispose();
}
