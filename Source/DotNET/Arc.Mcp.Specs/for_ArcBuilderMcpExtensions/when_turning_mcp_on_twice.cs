// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Mcp.for_McpToolCeiling.given;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;

namespace Cratis.Arc.Mcp.for_ArcBuilderMcpExtensions;

public class when_turning_mcp_on_twice : Specification
{
    ServiceCollection _services;
    McpOptions _defaults;
    McpExposure _exposure;

    void Because()
    {
        _services = [];
        var builder = Substitute.For<IArcBuilder>();
        builder.Services.Returns(_services);
        builder.WithMcp(mcp => mcp.Expose<DeclaredCommand>("first", "First."));
        builder.WithMcp(mcp => mcp.Configure(options => options.ApplicationName = "app"));
        using var provider = _services.BuildServiceProvider();
        _defaults = provider.GetRequiredService<IOptions<McpOptions>>().Value;
        _exposure = provider.GetRequiredService<McpExposure>();
    }

    [Fact] void should_default_to_the_mcp_path() => _defaults.Path.ShouldEqual("/mcp");
    [Fact] void should_default_to_the_pat_only_profile() => _defaults.Profile.ShouldEqual(McpProfile.PatOnly);
    [Fact] void should_derive_scopes_from_the_application_name() => (_defaults.ReadScope, _defaults.WriteScope).ShouldEqual(("app:read", "app:write"));
    [Fact] void should_share_one_exposure_between_calls() => _exposure.Declarations.Count.ShouldEqual(1);
    [Fact] void should_register_one_validation() => _services.Count(_ => _.ServiceType == typeof(IHostedService) && _.ImplementationType == typeof(McpStartupValidation)).ShouldEqual(1);
    [Fact] void should_register_one_ceiling_factory() => _services.Count(_ => _.ServiceType == typeof(McpToolCeilingFactory)).ShouldEqual(1);
}
