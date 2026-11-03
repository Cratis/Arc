// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Chronicle.Testing.Commands;
using Cratis.Arc.Testing.Commands;
using Cratis.Chronicle.EventSequences;

namespace Cratis.Arc.Chronicle.Commands.for_EventForEventSourceIdShouldExtensions.when_asserting_the_target;

public class and_it_targets_another_source : Specification
{
    Exception _error;
    void Because() => _error = Catch.Exception(() => new EventForEventSourceId("first", new given.ItemAdded(3)).ShouldTarget("second"));
    [Fact] void should_fail() => _error.ShouldBeOfExactType<CommandDecisionAssertionException>();
    [Fact] void should_name_the_expected_source() => _error.Message.ShouldContain("second");
    [Fact] void should_name_the_actual_source() => _error.Message.ShouldContain("first");
    [Fact] void should_describe_the_event() => _error.Message.ShouldContain("ItemAdded { Quantity = 3 }");
}
