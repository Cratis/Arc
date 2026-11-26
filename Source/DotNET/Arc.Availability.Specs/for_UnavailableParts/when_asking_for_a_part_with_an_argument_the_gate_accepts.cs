// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Extensions.DependencyInjection;

namespace Cratis.Arc.Availability.for_UnavailableParts;

public class when_asking_for_a_part_with_an_argument_the_gate_accepts : given_a_part_gated_on_an_argument
{
    IReadOnlyList<UnavailablePart> _unavailable;

    async Task Because() => _unavailable = await _scope.ServiceProvider.GetRequiredService<IUnavailableParts>()
        .Get(PartQuestion.From(typeof(UngatedCommand).FullName!, "{\"VesselId\":\"allowed\"}"));

    [Fact] void should_not_list_the_part() => _unavailable.ShouldBeEmpty();
}
