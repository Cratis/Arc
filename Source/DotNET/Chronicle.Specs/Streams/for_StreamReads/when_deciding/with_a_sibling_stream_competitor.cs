// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Chronicle.Testing.Commands;
using Cratis.Arc.Testing.Commands;

namespace Cratis.Arc.Chronicle.Streams.for_StreamReads.when_deciding;

public class with_a_sibling_stream_competitor : given.a_stream_decision
{
    void Establish()
    {
        _scenario.Given.ForEventSource(_id).OnRoute(_route).Events(new PriorFact("prior"));
        _scenario.AppendConcurrently(_id, _route.WithStreamId("sibling"), new PriorFact("competitor"));
    }
    async Task Because() => _result = await _scenario.Execute(new(_id));
    [Fact] void should_allow_the_unrelated_append() => _result.ShouldBeSuccessful();
    [Fact] void should_commit_one_owner_fact() => _scenario.AppendedEvents.Count.ShouldEqual(1);
}
