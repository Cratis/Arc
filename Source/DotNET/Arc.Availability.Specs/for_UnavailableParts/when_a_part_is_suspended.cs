// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Availability.Audiences;
using Cratis.Arc.Availability.Suspensions;
using Cratis.Arc.Commands.ModelBound;
using Microsoft.Extensions.DependencyInjection;

namespace Cratis.Arc.Availability.for_UnavailableParts;

public class when_a_part_is_suspended : given.an_availability_host
{
    IReadOnlyList<UnavailablePart> _unavailable;

    protected override IEnumerable<Type> Types => [typeof(PlusCommand), typeof(LooseCommand)];

    async Task Establish() => await _suspensions.Suspend(AudienceLevel.Tenant, "acme", PartPaths.For(typeof(LooseCommand)));

    async Task Because() => _unavailable = await _scope.ServiceProvider.GetRequiredService<IUnavailableParts>().Get();

    [Fact] void should_list_the_suspended_part_with_the_suspended_reason() => _unavailable.Single(_ => _.Name == typeof(LooseCommand).FullName).Reason.ShouldEqual(AvailabilityReason.Suspended);
    [Fact] void should_list_the_gated_part_with_the_not_available_reason() => _unavailable.Single(_ => _.Name == typeof(PlusCommand).FullName).Reason.ShouldEqual(AvailabilityReason.NotAvailable);

    [Command]
    public record LooseCommand;
}
