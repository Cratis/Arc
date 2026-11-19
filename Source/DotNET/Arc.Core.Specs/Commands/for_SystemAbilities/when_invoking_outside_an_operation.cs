// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Commands.Abilities;
using Cratis.Arc.Commands.for_SystemAbilities.given;

namespace Cratis.Arc.Commands.for_SystemAbilities;

public class when_invoking_outside_an_operation : Specification
{
    LedgerAbilities _ledger;
    Exception _result;

    void Establish() => _ledger = new(Substitute.For<IAbilityInvoker>(), Substitute.For<ICommandContextAccessor>());

    async Task Because() => _result = await Catch.Exception(() => _ledger.ReleaseRevenue("order-1", CancellationToken.None));

    [Fact] void should_be_rejected() => _result.ShouldBeOfExactType<InvalidCommandOperation>();
}
