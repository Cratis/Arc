// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;

namespace Cratis.Arc.Capabilities.for_CapabilitiesArcBuilderExtensions;

public class when_turning_capabilities_on : Specification
{
    ServiceCollection _services;
    CapabilitiesOptions _defaults;
    CapabilitiesOptions _configured;

    void Because()
    {
        _services = new();
        var builder = Substitute.For<IArcBuilder>();
        builder.Services.Returns(_services);
        builder.WithCapabilities();
        using (var provider = _services.BuildServiceProvider())
        {
            _defaults = provider.GetRequiredService<IOptions<CapabilitiesOptions>>().Value;
        }
        builder.WithCapabilities(options => options.EndpointGroup = "tools");
        using var configured = _services.BuildServiceProvider();
        _configured = configured.GetRequiredService<IOptions<CapabilitiesOptions>>().Value;
    }

    [Fact] void should_default_to_capabilities() => _defaults.EndpointGroup.ShouldEqual("capabilities");
    [Fact] void should_admit_people_and_delegated_actors() => _defaults.AllowedPrincipalKinds.ShouldContainOnly(CapabilityPrincipalKind.Person, CapabilityPrincipalKind.DelegatedActor);
    [Fact] void should_apply_configuration() => _configured.EndpointGroup.ShouldEqual("tools");
    [Fact] void should_register_one_middleware() => _services.Count(_ => _.ServiceType == typeof(CapabilityGateMiddleware)).ShouldEqual(1);
    [Fact] void should_register_one_validation() => _services.Count(_ => _.ServiceType == typeof(IHostedService) && _.ImplementationType == typeof(CapabilitiesStartupValidation)).ShouldEqual(1);
}
