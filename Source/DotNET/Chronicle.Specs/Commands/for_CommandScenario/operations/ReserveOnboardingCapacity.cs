// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Commands;

namespace Cratis.Arc.Chronicle.Commands.for_CommandScenario.operations;

public record ReserveOnboardingCapacity(Guid ReservationKey) : ICommandOperation
{
    public Task Execute(IOnboardingReservations reservations, CancellationToken cancellationToken) => reservations.Reserve(ReservationKey, cancellationToken);
    public Task Compensate(IOnboardingReservations reservations, CancellationToken cancellationToken) => reservations.Cancel(ReservationKey, cancellationToken);
}
