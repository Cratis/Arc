// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;
using Cratis.Chronicle.EventSequences;
using Cratis.Chronicle.EventSequences.Concurrency;
using Cratis.Chronicle.Transactions;

namespace Cratis.Arc.Chronicle.Commands.for_FreshIdentifierEnrollmentConflicts.when_mapping;

public class with_a_different_sequence : given.a_recorded_guard
{
    void Establish() => _conflict = new ConflictingEnrollmentScope(_id, ConcurrencyScope.None, new(EventSequenceNumber.BeforeFirst, EventSourceId: _id), new EventSequenceId("other-sequence"));
    void Because()
    {
        CommandTransaction.Current = _receivingUnit;
        _handled = _mapper.TryMap(_context, _conflict, out _mapped);
    }

    [Fact] void should_require_matching_sequence_provenance() => _handled.ShouldBeFalse();
    [Fact] void should_not_map_an_external_sequence() => _mapped.ShouldBeNull();
}
