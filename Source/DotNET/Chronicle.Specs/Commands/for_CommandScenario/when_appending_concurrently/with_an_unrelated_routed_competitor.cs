// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Chronicle.Streams;
using Cratis.Arc.Chronicle.Testing.Commands;
using Cratis.Arc.Testing.Commands;
using Cratis.Chronicle.Events;

namespace Cratis.Arc.Chronicle.Commands.for_CommandScenario.when_appending_concurrently;

public class with_an_unrelated_routed_competitor : given.a_concurrent_append_scenario
{
    readonly EventSourceId _other = EventSourceId.New();
    readonly EventRoute _route = new("competitors", "facts", "other");

    void Establish() => _scenario.AppendConcurrently(_other, _route, new FactRecorded("competitor"));
    async Task Because() => _result = await _scenario.Execute(new AppendWithExplicitScope(_source));

    [Fact] void should_succeed() => _result.ShouldBeSuccessful();
    [Fact] void should_capture_only_the_owner() => _scenario.AppendedEvents.Select(_ => _.Event.Content).ShouldContainOnly(new FactRecorded("owner"));
    [Fact] async Task should_preserve_the_competitor_route() => (await _scenario.EventLog.GetFromSequenceNumber(EventSequenceNumber.First, _other)).Single().Context.EventStreamId.ShouldEqual(_route.EventStreamId);
}
