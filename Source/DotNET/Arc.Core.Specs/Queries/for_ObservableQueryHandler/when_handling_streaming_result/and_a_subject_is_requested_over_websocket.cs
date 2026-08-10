// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reactive.Subjects;

namespace Cratis.Arc.Queries.for_ObservableQueryHandler.when_handling_streaming_result;

public class and_a_subject_is_requested_over_websocket : given.a_handler_over_a_real_container
{
    readonly BehaviorSubject<string> _subject = new("item-a");

    Exception _error;

    void Establish() => ConnectOverWebSocket();

    async Task Because() => _error = await Catch.Exception(() => _handler.HandleStreamingResult(_context, StreamingQueryName, _subject));

    [Fact] void should_construct_the_observable() => _error.ShouldBeNull();
    [Fact] void should_write_the_emission_to_the_client() => _sent.Count.ShouldEqual(1);
}
