// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.EventSequences;
using Cratis.Chronicle.EventSequences.Concurrency;

namespace Cratis.Arc.Chronicle.Commands.for_FreshIdentifierScopes.when_resolving;

public class with_a_conflicting_explicit_scope : Commands.given.a_generated_target
{
    Exception? _error;

    void Because() => _error = Catch.Exception(() => _freshness.Resolve(EventSequenceId.Log, _id, ConcurrencyScope.None, true));

    [Fact] void should_refuse_the_conflicting_intent() => _error.ShouldBeOfExactType<ConflictingConcurrencyScopes>();
    [Fact] void should_not_expose_the_generated_identifier_in_the_error() => _error!.Message.ShouldNotContain(_id.Value);
}
