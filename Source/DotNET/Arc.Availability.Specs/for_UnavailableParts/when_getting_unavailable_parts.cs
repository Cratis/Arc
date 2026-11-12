// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Choreography.Availability.Audiences;
using Microsoft.Extensions.DependencyInjection;

namespace Cratis.Arc.Availability.for_UnavailableParts;

public class when_getting_unavailable_parts : given.an_availability_host
{
    IReadOnlyList<UnavailablePart> _unavailable;

    async Task Establish() => await _audiences.Grant(AudienceLevel.Tenant, "acme", "Preview");

    async Task Because() => _unavailable = await _scope.ServiceProvider.GetRequiredService<IUnavailableParts>().Get();

    [Fact] void should_list_the_parts_that_are_not_available() => _unavailable.Select(part => part.Name).ShouldContainOnly(
        typeof(given.an_availability_host.PlusCommand).FullName,
        typeof(given.Fleet.Quality.ScoresReadModel).FullName);
}
