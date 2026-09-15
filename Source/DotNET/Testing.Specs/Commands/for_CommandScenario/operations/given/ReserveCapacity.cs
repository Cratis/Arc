// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Commands;

namespace Cratis.Arc.Testing.Commands.for_CommandScenario.operations.given;

/// <summary>
/// Tutorial example: the key identifies work owned by this logical request. The provider must make cancellation
/// safe even when creation is delayed or its acknowledgment is lost. No services are captured in this declaration.
/// </summary>
/// <param name="ReservationKey">Stable ownership key supplied by the caller.</param>
/// <param name="Resource">The capacity being reserved.</param>
/// <param name="Quantity">Required capacity.</param>
public sealed record ReserveCapacity(Guid ReservationKey, string Resource, int Quantity) : ICommandOperation
{
    public Task Execute(ICapacityReservations reservations, CancellationToken cancellationToken) =>
        reservations.Reserve(ReservationKey, Resource, Quantity, cancellationToken);

    public Task Compensate(ICapacityReservations reservations, CancellationToken cancellationToken) =>
        reservations.Cancel(ReservationKey, cancellationToken);
}
