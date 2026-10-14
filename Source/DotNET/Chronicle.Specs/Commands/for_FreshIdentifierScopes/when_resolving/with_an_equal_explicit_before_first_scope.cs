// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;
using Cratis.Chronicle.EventSequences;
using Cratis.Chronicle.EventSequences.Concurrency;

namespace Cratis.Arc.Chronicle.Commands.for_FreshIdentifierScopes.when_resolving;

public class with_an_equal_explicit_before_first_scope : Commands.given.a_generated_target
{
    void Establish() => _scope = _freshness.Resolve(EventSequenceId.Log, _id, null, false)!;

    void Because() => _scope = _freshness.Resolve(EventSequenceId.Log, _id, new(EventSequenceNumber.BeforeFirst, _id, EventStreamType.All, EventStreamId.Default, EventSourceType.Unspecified, []), true)!;

    [Fact] void should_merge_into_the_original_whole_source_intent() => _scope.ShouldEqual(new ConcurrencyScope(EventSequenceNumber.BeforeFirst, EventSourceId: _id));
}
