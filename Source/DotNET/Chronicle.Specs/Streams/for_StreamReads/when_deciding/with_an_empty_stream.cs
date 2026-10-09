// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Chronicle.Testing.Commands;
using Cratis.Arc.Testing.Commands;

namespace Cratis.Arc.Chronicle.Streams.for_StreamReads.when_deciding;

public class with_an_empty_stream : given.a_stream_decision
{
    async Task Because() => _result = await _scenario.Execute(new(_id));
    [Fact] void should_allow_the_first_append() => _result.ShouldBeSuccessful();
    [Fact] void should_report_the_unavailable_tail_as_empty() => ((Approved)_scenario.AppendedEvents.Single().Event.Content).WasEmpty.ShouldBeTrue();
}
