// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;

namespace Cratis.Arc.Chronicle.Streams.for_EventRoute;

public class when_completing : Specification
{
    EventRoute _route;
    CompleteStream _completion;

    void Establish() => _route = new EventRoute("reports", "approval", "owner:month");

    void Because() => _completion = _route.Complete();

    [Fact] void should_complete_the_route_stream_type() => _completion.EventStreamType.ShouldEqual(_route.EventStreamType);
    [Fact] void should_complete_the_route_stream_id() => _completion.EventStreamId.ShouldEqual(_route.EventStreamId);
}
