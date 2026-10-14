// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;
using Cratis.Chronicle.EventSequences;
using Cratis.Chronicle.EventSequences.Concurrency;

namespace Cratis.Arc.Chronicle.Commands.for_FreshIdentifierScopes.when_resolving;

public class with_another_sequence : Commands.given.a_generated_target
{
    ConcurrencyScope _other;

    void Establish() => _scope = _freshness.Resolve(EventSequenceId.Log, _id, null, false)!;

    void Because() => _other = _freshness.Resolve(new EventSequenceId("another-sequence"), _id, null, false)!;

    [Fact] void should_keep_sequence_registries_independent() => _other.ShouldNotBeSame(_scope);
    [Fact] void should_guard_the_second_sequence() => _other.SequenceNumber.ShouldEqual(EventSequenceNumber.BeforeFirst);
}
