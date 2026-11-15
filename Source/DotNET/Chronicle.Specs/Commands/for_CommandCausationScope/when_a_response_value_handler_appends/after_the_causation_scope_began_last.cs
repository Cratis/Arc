// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Chronicle.Streams;
using Cratis.Arc.Commands;

namespace Cratis.Arc.Chronicle.Commands.for_CommandCausationScope.when_a_response_value_handler_appends;

/// <summary>
/// The transactional scope began first, so it completes last: the deferred work runs after the causation scope completed.
/// </summary>
public class after_the_causation_scope_began_last : given.a_command_with_a_response_value_handler
{
    async Task Because() => await Execute(
        [new TransactionalCommandScope(), new CommandCausationScope()],
        () => new CompleteStreamCommandResponseValueHandler(_eventLog).Handle(_context, new CompleteStream("completion", "period")));

    [Fact] void should_have_the_command_on_the_chain_when_the_deferred_append_happens() => CommandLink(_chainAtCompletion).ShouldNotBeNull();
    [Fact] void should_have_the_command_on_the_chain_when_the_unit_of_work_commits() => CommandLink(_chainAtCommit).ShouldNotBeNull();
    [Fact] void should_carry_the_same_properties_as_a_returned_event() =>
        CommandLink(_chainAtCompletion).Properties.ShouldContainOnly(CommandCausation.PropertiesFor(typeof(PayInvoice), _command));
}
