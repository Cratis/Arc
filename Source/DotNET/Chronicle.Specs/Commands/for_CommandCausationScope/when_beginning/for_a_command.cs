// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using Cratis.Chronicle.Auditing;

namespace Cratis.Arc.Chronicle.Commands.for_CommandCausationScope.when_beginning;

public class for_a_command : given.a_command_causation_scope
{
    IImmutableList<Causation> _chain;

    void Because()
    {
        _scope.Begin(ContextFor<ApproveExpenseReport>());
        _chain = _causationManager.GetCurrentChain();
    }

    [Fact] void should_add_a_link_on_top_of_the_root() => _chain.Count.ShouldEqual(2);
    [Fact] void should_record_it_as_a_command() => _chain[^1].Type.ShouldEqual(CommandCausation.Type);
    [Fact] void should_name_the_command() => _chain[^1].Properties[CommandCausation.CommandTypeProperty].ShouldEqual(nameof(ApproveExpenseReport));
    [Fact] void should_qualify_the_command_name() => _chain[^1].Properties[CommandCausation.CommandTypeFullNameProperty].ShouldEqual(typeof(ApproveExpenseReport).FullName);
}
