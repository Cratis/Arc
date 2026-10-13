// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Chronicle.Reactors.for_CommandInvocationResultHandler.when_dispatching;

public class a_single_invocation : given.an_invocation_dispatcher
{
    async Task Because() => _result = await _dispatcher.Handle(_context, _store, _invocation);

    [Fact] void should_support_synchronous_invocation_discovery() => _dispatcher.CanHandleReturnType(typeof(CommandInvocation)).ShouldBeTrue();
    [Fact] void should_succeed() => _result.IsSuccess.ShouldBeTrue();
    [Fact] void should_append_the_branch_event() => _appended.Single().Event.ShouldEqual(new RefusalRecorded("$strings.refused"));
}
