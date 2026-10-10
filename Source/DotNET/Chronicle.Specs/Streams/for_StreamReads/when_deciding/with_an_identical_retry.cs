// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Chronicle.Testing.Commands;
using Cratis.Arc.Testing.Commands;

namespace Cratis.Arc.Chronicle.Streams.for_StreamReads.when_deciding;

public class with_an_identical_retry : given.a_stream_decision
{
    void Establish() => _scenario.AppendConcurrently(_id, _route, new PriorFact("competitor"));
    async Task Because() => _result = await _scenario.Execute(new(_id, Retry: true));
    [Fact] void should_succeed_without_a_guard_or_new_facts() => _result.ShouldBeSuccessful();
    [Fact] void should_append_no_owner_facts() => _scenario.AppendedEvents.ShouldBeEmpty();
}
