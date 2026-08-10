// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Queries.for_ClientObservableSSE.when_handling_connection;

/// <summary>
/// An application that never implements a guard must not pay for the seam. Nothing else pins that on this transport,
/// so removing the <see cref="IObservableQueryEmissionGuards.HasGuards"/> gate would stay green while charging every
/// existing consumer a dispatch on every emission.
/// </summary>
public class and_no_guard_is_registered : given.a_guarded_client_observable_sse
{
    void Establish() => _emissionGuards.HasGuards.Returns(false);

    async Task Because() => await RunConnection(async () =>
    {
        _subject.OnNext("event-store-a");
        await WaitFor(() => _messages.Count == 1);
    });

    [Fact] void should_write_the_emission() => WrittenResults.Single().Data.ToString().ShouldEqual("event-store-a");
    [Fact] void should_not_dispatch_to_the_guards() => _emissionGuards.DidNotReceive().Guard(Arg.Any<ObservableQueryEmissionContext>());
}
