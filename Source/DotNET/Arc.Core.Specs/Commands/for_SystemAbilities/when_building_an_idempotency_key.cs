// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Commands.for_SystemAbilities.given;

namespace Cratis.Arc.Commands.for_SystemAbilities;

public class when_building_an_idempotency_key : Specification
{
    string _first;
    string _again;
    string _otherCall;
    string _otherCommand;

    void Because()
    {
        using (CommandOperationPhaseScope.Enter(typeof(ReserveOrderRevenue), 0, CommandOperationPhase.Execute))
        {
            var scope = CommandOperationPhaseScope.Current;
            _first = Abilities.SystemAbilities.IdempotencyKey("c1", scope, "Ledger", "ReserveRevenue", 0);
            _again = Abilities.SystemAbilities.IdempotencyKey("c1", scope, "Ledger", "ReserveRevenue", 0);
            _otherCall = Abilities.SystemAbilities.IdempotencyKey("c1", scope, "Ledger", "ReserveRevenue", 1);
            _otherCommand = Abilities.SystemAbilities.IdempotencyKey("c2", scope, "Ledger", "ReserveRevenue", 0);
        }
    }

    [Fact] void should_be_stable_for_the_same_call() => _again.ShouldEqual(_first);
    [Fact] void should_differ_per_call() => _otherCall.ShouldNotEqual(_first);
    [Fact] void should_differ_per_command_execution() => _otherCommand.ShouldNotEqual(_first);
    [Fact] void should_leave_no_phase_behind() => CommandOperationPhaseScope.Current.ShouldBeNull();
}
