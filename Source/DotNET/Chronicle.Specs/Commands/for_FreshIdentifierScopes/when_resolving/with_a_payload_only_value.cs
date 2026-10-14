// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;
using Cratis.Chronicle.EventSequences;
using Cratis.Chronicle.EventSequences.Concurrency;

namespace Cratis.Arc.Chronicle.Commands.for_FreshIdentifierScopes.when_resolving;

public class with_a_payload_only_value : Commands.given.a_generated_target
{
    ConcurrencyScope? _resolved;

    void Because() => _resolved = _freshness.Resolve(EventSequenceId.Log, new EventSourceId("another-target"), null, false);

    [Fact] void should_not_enroll_a_scope_for_a_value_that_is_not_the_append_target() => _resolved.ShouldBeNull();
}
