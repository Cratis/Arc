// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Testing.Commands.for_CommandScenario.operations.given;

/// <summary>
/// Reserve and Cancel use a stable ownership key. A canceled key cannot later create a live reservation.
/// </summary>
public interface ICapacityReservations
{
    Task Reserve(Guid reservationKey, string resource, int quantity, CancellationToken cancellationToken);
    Task Cancel(Guid reservationKey, CancellationToken cancellationToken);
}
