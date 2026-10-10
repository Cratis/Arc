// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Chronicle.Testing.Commands;
using Cratis.Arc.Testing.Commands;

namespace Cratis.Arc.Chronicle.Streams.for_StreamReads.when_deciding;

public class without_decision_seeding : given.a_stream_decision
{
    CommandScenario<DecideStream> _legacy;
    Exception? _error;
    void Establish() => _legacy = new();
    void Because() => _error = Catch.Exception(() => _legacy.Given.ForEventSource(_id).OnRoute(_route).Events(new PriorFact("prior")));
    void Destroy() => _legacy.Dispose();
    [Fact] void should_refuse_to_discard_the_seed_route() => _error.ShouldBeOfExactType<RoutedEventSeedingRequiresDecisionReads>();
}
