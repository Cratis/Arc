// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Commands.for_CommandOperationExecution.given;
using Cratis.Arc.Commands.for_SystemAbilities.given;

namespace Cratis.Arc.Commands.for_SystemAbilities;

public class when_an_operation_invokes_an_ability : an_ability_operation_pipeline
{
    async Task Because() => _result = await Run(new OperationCommand([new ReserveOrderRevenue("order-1", 42m)]));

    [Fact] void should_succeed() => _result.IsSuccess.ShouldBeTrue();
    [Fact] void should_run_the_ability_once() => _invoker.Invocations.Count.ShouldEqual(1);
    [Fact] void should_name_the_system() => _invoker.Invocations[0].System.ShouldEqual("Ledger");
    [Fact] void should_name_the_ability() => _invoker.Invocations[0].Ability.ShouldEqual("ReserveRevenue");
    [Fact] void should_pass_the_inputs() => _invoker.Invocations[0].Inputs["orderId"].ShouldEqual("order-1");
    [Fact] void should_pass_the_typed_amount() => _invoker.Invocations[0].Inputs["amount"].ShouldEqual(42m);
    [Fact] void should_carry_the_correlation_in_the_causation() => _invoker.Invocations[0].Causation["correlationId"].ShouldEqual(_correlationId.ToString());
    [Fact] void should_carry_the_operation_in_the_causation() => _invoker.Invocations[0].Causation["operation"].ShouldEqual(typeof(ReserveOrderRevenue).FullName);
    [Fact] void should_carry_the_phase_in_the_causation() => _invoker.Invocations[0].Causation["phase"].ShouldEqual("execute");
    [Fact] void should_give_an_idempotency_key() => _invoker.Invocations[0].IdempotencyKey.ShouldNotBeEmpty();
}
