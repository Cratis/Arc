// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Commands;

namespace Cratis.Arc.Chronicle.Commands.for_TransactionalCommandScope.when_executing_for_an_occurrence;

public class and_refusal_disposal_throws : given.an_occurrence_execution
{
    bool _invocationExited;

    void Because()
    {
        BeginExecution();
        _scope.Begin(_context);
        _unit = CommandTransaction.Current!;
        _unit.OnCompleted(_ => throw new InvalidCommandOperation("completion callback failed"));
        _exception = Catch.Exception(() => _scope.End(_context));
        _invocationExited = TransactionalCommandScope.CurrentFreshIdentifiers is null;
    }

    [Fact] void should_surface_the_disposal_failure() => _exception.ShouldBeOfExactType<InvalidCommandOperation>();
    [Fact] void should_still_exit_the_invocation() => _invocationExited.ShouldBeTrue();
    [Fact] void should_roll_back_the_unit() => _unit.IsCompleted.ShouldBeTrue();
}
