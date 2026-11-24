// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Availability.Audiences;

namespace Cratis.Arc.Availability.for_AudienceResolver;

public class when_the_switch_is_turned_on_in_production_after_creation : Specification
{
    InMemoryAudienceSource _source = new();
    Exception _error;

    async Task Because()
    {
        var options = new AvailabilityOptions { EnvironmentName = "Production" };
        var resolver = new AudienceResolver(_source, new DeclaredAudiences("Plus"), options);
        options.GrantAllDeclaredAudiences = true;
        _error = await Catch.Exception(async () => await resolver.Resolve(new AudienceSubject("t1", null)));
    }

    [Fact] void should_refuse_to_resolve() => _error.ShouldBeOfExactType<EnvironmentSwitchRefusedException>();
}
