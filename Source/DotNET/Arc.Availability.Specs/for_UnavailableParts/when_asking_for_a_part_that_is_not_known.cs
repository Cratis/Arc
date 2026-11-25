// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Extensions.DependencyInjection;

namespace Cratis.Arc.Availability.for_UnavailableParts;

public class when_asking_for_a_part_that_is_not_known : given_a_part_gated_on_an_argument
{
    IReadOnlyList<UnavailablePart> _unavailable;

    async Task Because() => _unavailable = await _scope.ServiceProvider.GetRequiredService<IUnavailableParts>()
        .Get(PartQuestion.From("Not.A.Part", null));

    [Fact] void should_not_list_anything() => _unavailable.ShouldBeEmpty();
}
