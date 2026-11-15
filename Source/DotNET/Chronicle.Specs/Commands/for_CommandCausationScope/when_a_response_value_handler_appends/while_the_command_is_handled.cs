// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using Cratis.Arc.Commands;
using Cratis.Chronicle.Auditing;

namespace Cratis.Arc.Chronicle.Commands.for_CommandCausationScope.when_a_response_value_handler_appends;

public class while_the_command_is_handled : given.a_command_with_a_response_value_handler
{
    IImmutableList<Causation> _chainWhileHandling;

    async Task Because() => await Execute(
        [new TransactionalCommandScope(), new CommandCausationScope()],
        () =>
        {
            _chainWhileHandling = _causationManager.GetCurrentChain();
            return Task.CompletedTask;
        });

    [Fact] void should_have_the_command_on_the_chain() => CommandLink(_chainWhileHandling).ShouldNotBeNull();
    [Fact] void should_name_the_command() => CommandLink(_chainWhileHandling).Properties[CommandCausation.CommandTypeProperty].ShouldEqual(nameof(PayInvoice));
    [Fact] void should_carry_the_same_properties_as_a_returned_event() =>
        CommandLink(_chainWhileHandling).Properties.ShouldContainOnly(CommandCausation.PropertiesFor(typeof(PayInvoice), _command));
}
