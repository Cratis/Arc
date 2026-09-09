// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Queries.for_ClientObservable.when_handling_connection;

/// <summary>
/// A single-document observable query emits <see langword="null"/> to report "no such document" — after the
/// observed document is deleted, updated out of the filter, or never found. The default multiplexed path
/// (<see cref="ObservableQueryDemultiplexer"/>) already forwards a null emission unconditionally; direct-mode
/// WebSocket must match rather than silently dropping it and leaving the client waiting forever.
/// </summary>
public class and_the_emitted_value_is_null : given.a_guarded_client_observable
{
    void Establish() => _emissionGuards.HasGuards.Returns(false);

    async Task Because() => await RunConnection(async () =>
    {
        _subject.OnNext(null!);
        await WaitFor(() => _sent.Count == 1);
    });

    [Fact] void should_send_a_result() => _sent.Count.ShouldEqual(1);
    [Fact] void should_send_null_data() => _sent.Single().Data.ShouldBeNull();
    [Fact] void should_not_dispatch_to_the_guards() => _emissionGuards.DidNotReceive().Guard(Arg.Any<ObservableQueryEmissionContext>());
}
