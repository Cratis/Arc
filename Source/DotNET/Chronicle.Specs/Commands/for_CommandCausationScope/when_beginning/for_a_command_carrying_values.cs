// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using Cratis.Chronicle.Auditing;

namespace Cratis.Arc.Chronicle.Commands.for_CommandCausationScope.when_beginning;

/// <summary>
/// The scope is what puts the command on the chain for every append it makes, so the values have to travel with it
/// there and not only on the events a handler returns.
/// </summary>
public class for_a_command_carrying_values : given.a_command_causation_scope
{
    sealed record RejectExpenseReport(string Reason);

    IImmutableList<Causation> _chain;

    void Because()
    {
        _scope.Begin(ContextFor(new RejectExpenseReport("Missing receipts")));
        _chain = _causationManager.GetCurrentChain();
    }

    [Fact] void should_still_name_the_command() =>
        _chain[^1].Properties[CommandCausation.CommandTypeProperty].ShouldEqual(nameof(RejectExpenseReport));

    [Fact] void should_record_what_the_command_was_asked_to_do() =>
        _chain[^1].Properties["reason"].ShouldEqual("Missing receipts");
}
