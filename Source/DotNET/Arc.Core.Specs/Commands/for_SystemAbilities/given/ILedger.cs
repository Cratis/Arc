// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Commands.for_SystemAbilities.given;

public interface ILedger
{
    Task ReserveRevenue(string orderId, decimal amount, CancellationToken cancellationToken);

    Task ReleaseRevenue(string orderId, CancellationToken cancellationToken);
}
