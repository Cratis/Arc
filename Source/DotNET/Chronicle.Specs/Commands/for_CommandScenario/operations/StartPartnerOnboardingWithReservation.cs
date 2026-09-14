// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Commands.ModelBound;
using Cratis.Chronicle.Events;

namespace Cratis.Arc.Chronicle.Commands.for_CommandScenario.operations;

[Command]
public record StartPartnerOnboardingWithReservation(EventSourceId EventSourceId, string OrganizationNumber, Guid ReservationKey)
{
    public (PartnerOnboardingStarted Event, ReserveOnboardingCapacity Operation) Handle() =>
        (new(OrganizationNumber), new(ReservationKey));
}
