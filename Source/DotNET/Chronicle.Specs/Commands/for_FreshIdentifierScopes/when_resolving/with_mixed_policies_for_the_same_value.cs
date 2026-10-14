// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;
using Cratis.Chronicle.EventSequences;

namespace Cratis.Arc.Chronicle.Commands.for_FreshIdentifierScopes.when_resolving;

public class with_mixed_policies_for_the_same_value : Commands.given.a_generated_target
{
    async Task Establish() => await Capture<CreatesMixedPolicyIds>();

    void Because() => _scope = FreshIdentifierScopes.For(_context).Resolve(EventSequenceId.Log, _id, null, false)!;

    [Fact] void should_guard_if_any_capture_is_guarded() => _scope.SequenceNumber.ShouldEqual(EventSequenceNumber.BeforeFirst);
}
