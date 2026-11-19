// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Commands.for_SystemAbilities.given;

public record ReserveOrderRevenue(string OrderId, decimal Amount) : ICommandOperation
{
    public Task Execute(ILedger ledger, CancellationToken cancellationToken) => ledger.ReserveRevenue(OrderId, Amount, cancellationToken);

    public Task Compensate(ILedger ledger, CancellationToken cancellationToken) => ledger.ReleaseRevenue(OrderId, cancellationToken);
}
