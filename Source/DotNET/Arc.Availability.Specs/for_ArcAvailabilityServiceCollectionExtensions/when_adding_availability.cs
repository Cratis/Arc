// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Availability.Audiences;
using Microsoft.Extensions.DependencyInjection;

namespace Cratis.Arc.Availability.for_ArcAvailabilityServiceCollectionExtensions;

public class when_adding_availability : Specification
{
    IServiceProvider _provider;

    void Because()
    {
        var services = new ServiceCollection();
        services.AddSingleton<IAudienceSource>(new InMemoryAudienceSource());
        services.AddSingleton<IDeclaredAudiences>(new DeclaredAudiences("Plus"));
        services.AddSingleton(new AvailabilityCatalog());
        services.AddAvailability();
        _provider = services.BuildServiceProvider();
    }

    [Fact] void should_resolve_the_evaluator() => _provider.GetService(typeof(IAvailabilityEvaluator)).ShouldBeOfExactType<AvailabilityEvaluator>();
    [Fact] void should_resolve_the_audience_resolver() => _provider.GetService(typeof(IAudienceResolver)).ShouldBeOfExactType<AudienceResolver>();
    [Fact] void should_leave_the_switch_off() => ((AvailabilityOptions)_provider.GetService(typeof(AvailabilityOptions))!).GrantAllDeclaredAudiences.ShouldBeFalse();
}
