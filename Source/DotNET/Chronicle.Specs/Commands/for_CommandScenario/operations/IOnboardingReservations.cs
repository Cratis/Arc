// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Chronicle.Commands.for_CommandScenario.operations;

public interface IOnboardingReservations
{
    Task Reserve(Guid reservationKey, CancellationToken cancellationToken);
    Task Cancel(Guid reservationKey, CancellationToken cancellationToken);
}
