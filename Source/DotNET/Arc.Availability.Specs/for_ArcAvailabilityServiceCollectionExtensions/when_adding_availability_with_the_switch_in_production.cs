// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
using Microsoft.Extensions.DependencyInjection;

namespace Cratis.Arc.Availability.for_ArcAvailabilityServiceCollectionExtensions;

public class when_adding_availability_with_the_switch_in_production : Specification
{
    Exception _error;

    void Because() => _error = Catch.Exception(() =>
        new ServiceCollection().AddAvailability(_ =>
        {
            _.EnvironmentName = "Production";
            _.GrantAllDeclaredAudiences = true;
        }));

    [Fact] void should_refuse_at_startup() => _error.ShouldBeOfExactType<EnvironmentSwitchRefusedException>();
}
