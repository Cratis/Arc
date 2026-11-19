// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Commands.Abilities;

namespace Cratis.Arc.Commands.for_SystemAbilities.given;

public class LedgerAbilities(IAbilityInvoker invoker, ICommandContextAccessor contextAccessor) : SystemAbilities(invoker, contextAccessor), ILedger
{
    protected override string System => "Ledger";

    public Task ReserveRevenue(string orderId, decimal amount, CancellationToken cancellationToken) =>
        Invoke("ReserveRevenue", new Dictionary<string, object?> { ["orderId"] = orderId, ["amount"] = amount }, cancellationToken);

    public Task ReleaseRevenue(string orderId, CancellationToken cancellationToken) =>
        Invoke("ReleaseRevenue", new Dictionary<string, object?> { ["orderId"] = orderId }, cancellationToken);
}
