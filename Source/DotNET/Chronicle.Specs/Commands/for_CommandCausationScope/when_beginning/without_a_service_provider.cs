// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Commands;
using Cratis.Execution;

namespace Cratis.Arc.Chronicle.Commands.for_CommandCausationScope.when_beginning;

/// <summary>
/// A command executed without a service provider has nothing to record causation through. That is not a failure -
/// causation is metadata, and losing it must never take the command down with it.
/// </summary>
public class without_a_service_provider : given.a_command_causation_scope
{
    Exception _exception;

    void Because() => _exception = Catch.Exception(() =>
        _scope.Begin(new CommandContext(CorrelationId.New(), typeof(ApproveExpenseReport), new object(), [], new())));

    [Fact] void should_not_throw() => _exception.ShouldBeNull();
    [Fact] void should_not_record_a_command() => _causationManager.GetCurrentChain().Any(_ => _.Type == CommandCausation.Type).ShouldBeFalse();
}
