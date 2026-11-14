// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using Cratis.Chronicle.Auditing;

namespace Cratis.Arc.Chronicle.Commands.for_CommandCausationScope.when_a_response_value_handler_appends;

public class outside_any_command : given.a_command_with_a_response_value_handler
{
    IImmutableList<Causation> _chain;

    void Because() => _chain = _causationManager.GetCurrentChain();

    [Fact] void should_not_have_a_command_on_the_chain() => _chain.Any(_ => _.Type == CommandCausation.Type).ShouldBeFalse();
}
