// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Availability.Audiences;

namespace Cratis.Arc.Availability.for_AudienceResolver;

public class when_the_environment_switch_is_on_in_an_unnamed_environment : Specification
{
    Exception _error;

    void Because() => _error = Catch.Exception(() =>
    {
        _ = new AudienceResolver(
            new InMemoryAudienceSource(),
            new DeclaredAudiences("Plus"),
            new AvailabilityOptions { GrantAllDeclaredAudiences = true });
    });

    [Fact] void should_be_refused() => _error.ShouldBeOfExactType<EnvironmentSwitchRefusedException>();
}
