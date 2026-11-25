// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Availability.Audiences;
using Cratis.Types;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Cratis.Arc.Availability.for_ArcBuilderAvailabilityExtensions;

public class when_adding_availability_with_the_builder : Specification
{
    ServiceProvider _provider;
    IArcBuilder _result;
    IArcBuilder _builder;

    void Establish()
    {
        var services = new ServiceCollection();
        var types = Substitute.For<ITypes>();
        types.All.Returns([]);
        services.AddSingleton(types);
        _builder = Substitute.For<IArcBuilder>();
        _builder.Services.Returns(services);
    }

    void Because()
    {
        _result = _builder.WithAvailability();
        _provider = _builder.Services.BuildServiceProvider(true);
    }

    void Destroy() => _provider.Dispose();

    [Fact] void should_return_the_builder() => _result.ShouldEqual(_builder);
    [Fact] void should_default_the_audience_source_to_in_memory() => _provider.GetRequiredService<IAudienceSource>().ShouldBeOfExactType<InMemoryAudienceSource>();
    [Fact] void should_register_the_gate() => _provider.GetRequiredService<IAvailabilityGate>().ShouldBeOfExactType<ArcAvailabilityGate>();
    [Fact] void should_validate_at_startup() => _provider.GetServices<IHostedService>().OfType<AvailabilityStartupValidation>().ShouldNotBeEmpty();
    [Fact] void should_share_one_catalog() => _provider.GetRequiredService<AvailabilityCatalog>().ShouldEqual(_provider.GetRequiredService<AvailabilityCatalog>());
}
