// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Commands;
using Cratis.Execution;

namespace Cratis.Arc.Chronicle.Reactors.for_CommandSideEffectExecutor.when_handling_refusals;

public class with_authorization : given.a_refused_invocation
{
    void Establish()
    {
        _commandResult = CommandResult.Unauthorized(CorrelationId.NotSet, "denied");
        _invocation = Invoke.Command(_command).OnRefused(Refused.Any, _ => null).OnRefused(Refused.ByAuthorization, Record);
    }
    async Task Because() => _result = await _executor.Execute([_invocation], _context, _store);

    [Fact] void should_succeed() => _result.IsSuccess.ShouldBeTrue();
    [Fact] void should_use_the_explicit_authorization_branch() => _received!.Kind.ShouldEqual(CommandRefusalKind.Authorization);
    [Fact] void should_preserve_the_denial_reason() => _received!.Message.ShouldEqual("denied");
}
