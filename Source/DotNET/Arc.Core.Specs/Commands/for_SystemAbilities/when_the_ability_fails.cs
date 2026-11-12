// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Commands.for_CommandOperationExecution.given;
using Cratis.Arc.Commands.for_SystemAbilities.given;

namespace Cratis.Arc.Commands.for_SystemAbilities;

public class when_the_ability_fails : an_ability_operation_pipeline
{
    void Establish() => _invoker.FailAbility = "ReserveRevenue";

    async Task Because() => _result = await Run(new OperationCommand([new ReserveOrderRevenue("order-1", 42m)]));

    [Fact] void should_fail_the_command() => _result.IsSuccess.ShouldBeFalse();
    [Fact] void should_report_the_failure() => _result.ExceptionMessages.ShouldContain("Ability 'Ledger.ReserveRevenue' failed: ledger unavailable");
    [Fact] void should_compensate_with_the_declared_ability() => _invoker.Invocations.Select(_ => _.Ability).ToArray().ShouldEqual(["ReserveRevenue", "ReleaseRevenue"]);
    [Fact] void should_compensate_in_the_compensate_phase() => _invoker.Invocations[1].Causation["phase"].ShouldEqual("compensate");
    [Fact] void should_not_reuse_the_execute_key_for_compensation() => _invoker.Invocations[1].IdempotencyKey.ShouldNotEqual(_invoker.Invocations[0].IdempotencyKey);
}
