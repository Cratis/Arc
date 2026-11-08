// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Capabilities;
using Cratis.Arc.Commands;
using Cratis.Arc.Queries;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Cratis.Arc.Mcp.for_McpStartupValidation.given;

public class a_startup_validation : Specification
{
    protected ArcOptions _arc;
    protected ServiceCollection _services;
    protected Action<McpBuilder> _mcp;
    protected bool _withCapabilities;
    protected bool _withGatedEndpoints;
    protected Exception? _error;
    ServiceProvider _provider;

    void Establish()
    {
        _arc = new ArcOptions();
        _arc.EndpointGroups.Assign("capabilities", 42001);
        _services = [];
        _mcp = _ => { };
        _withCapabilities = true;
        _withGatedEndpoints = true;
    }

    protected void Validate()
    {
        var builder = Substitute.For<IArcBuilder>();
        builder.Services.Returns(_services);
        if (_withCapabilities)
        {
            builder.WithCapabilities(capabilities => capabilities.GatedEndpoints = _withGatedEndpoints ? new CapabilityEndpointSelector().ForRoutePattern("/other") : null);
        }
        builder.WithMcp(_mcp);
        _services.AddSingleton(Options.Create(_arc));
        _services.AddSingleton(Substitute.For<ICommandHandlerProviders>());
        _services.AddSingleton(Substitute.For<IQueryPerformerProviders>());
        _provider = _services.BuildServiceProvider();
        var validation = ActivatorUtilities.CreateInstance<McpStartupValidation>(_provider);
        _error = Catch.Exception(() => validation.StartingAsync(CancellationToken.None).GetAwaiter().GetResult());
    }

    void Destroy() => _provider.Dispose();
}
