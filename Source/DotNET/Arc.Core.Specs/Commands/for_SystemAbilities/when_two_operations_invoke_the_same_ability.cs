// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Commands.for_CommandOperationExecution.given;
using Cratis.Arc.Commands.for_SystemAbilities.given;

namespace Cratis.Arc.Commands.for_SystemAbilities;

public class when_two_operations_invoke_the_same_ability : an_ability_operation_pipeline
{
    async Task Because() => _result = await Run(new OperationCommand([new ReserveOrderRevenue("order-1", 1m), new ReserveOrderRevenue("order-1", 1m)]));

    [Fact] void should_run_both() => _invoker.Invocations.Count.ShouldEqual(2);
    [Fact] void should_give_each_its_own_key() => _invoker.Invocations[1].IdempotencyKey.ShouldNotEqual(_invoker.Invocations[0].IdempotencyKey);
}
