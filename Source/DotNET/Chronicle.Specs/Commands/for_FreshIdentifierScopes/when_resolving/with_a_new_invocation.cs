// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.EventSequences;

namespace Cratis.Arc.Chronicle.Commands.for_FreshIdentifierScopes.when_resolving;

public class with_a_new_invocation : Commands.given.a_generated_target
{
    bool _matches;

    async Task Establish() => await Capture<CreatesUnguardedId>();

    void Because() => _matches = FreshIdentifierScopes.For(_context).Resolve(EventSequenceId.Log, _id, null, false) is not null;

    [Fact] void should_not_leak_prior_guarded_captures() => _matches.ShouldBeFalse();
}
