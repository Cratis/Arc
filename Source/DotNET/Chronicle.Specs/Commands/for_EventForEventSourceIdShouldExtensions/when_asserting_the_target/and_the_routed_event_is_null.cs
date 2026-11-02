// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Chronicle.Testing.Commands;
using Cratis.Arc.Testing.Commands;
using Cratis.Chronicle.EventSequences;

namespace Cratis.Arc.Chronicle.Commands.for_EventForEventSourceIdShouldExtensions.when_asserting_the_target;

public class and_the_routed_event_is_null : Specification
{
    Exception _error;
    void Because() => _error = Catch.Exception(() => ((EventForEventSourceId)null!).ShouldTarget("first"));
    [Fact] void should_fail() => _error.ShouldBeOfExactType<CommandDecisionAssertionException>();
    [Fact] void should_identify_null() => _error.Message.ShouldContain("null");
}
