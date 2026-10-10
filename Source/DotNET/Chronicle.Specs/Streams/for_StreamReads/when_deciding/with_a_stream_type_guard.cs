// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Chronicle.Testing.Commands;
using Cratis.Arc.Testing.Commands;
using Cratis.Arc.Validation;

namespace Cratis.Arc.Chronicle.Streams.for_StreamReads.when_deciding;

public class with_a_stream_type_guard : given.a_stream_decision
{
    void Establish() => _scenario.AppendConcurrently(_id, _route.WithStreamId("sibling"), new PriorFact("competitor"));
    async Task Because() => _result = await _scenario.Execute(new(_id, WholeType: true));
    [Fact] void should_guard_sibling_stream_ids_too() => _result.ShouldHaveValidationErrorBecauseOf(ValidationResultReason.ConcurrencyViolation);
    [Fact] void should_not_commit_owner_facts() => _scenario.AppendedEvents.ShouldBeEmpty();
}
