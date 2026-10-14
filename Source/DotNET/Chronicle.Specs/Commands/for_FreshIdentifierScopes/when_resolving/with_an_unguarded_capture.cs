// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.EventSequences;
using Cratis.Chronicle.EventSequences.Concurrency;

namespace Cratis.Arc.Chronicle.Commands.for_FreshIdentifierScopes.when_resolving;

public class with_an_unguarded_capture : Commands.given.a_generated_target
{
    ConcurrencyScope? _resolved;

    async Task Establish() => await Capture<CreatesUnguardedId>();

    void Because() => _resolved = FreshIdentifierScopes.For(_context).Resolve(EventSequenceId.Log, _id, null, false);

    [Fact] void should_leave_the_target_unguarded() => _resolved.ShouldBeNull();
}
