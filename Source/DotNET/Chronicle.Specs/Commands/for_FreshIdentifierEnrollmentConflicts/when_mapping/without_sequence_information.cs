// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;
using Cratis.Chronicle.EventSequences.Concurrency;
using Cratis.Chronicle.Transactions;

namespace Cratis.Arc.Chronicle.Commands.for_FreshIdentifierEnrollmentConflicts.when_mapping;

public class without_sequence_information : given.a_recorded_guard
{
    void Establish() => _conflict = new ConflictingEnrollmentScope(_id, ConcurrencyScope.None, new(EventSequenceNumber.BeforeFirst, EventSourceId: _id));
    void Because()
    {
        CommandTransaction.Current = _receivingUnit;
        _handled = _mapper.TryMap(_context, _conflict, out _mapped);
    }

    [Fact] void should_not_guess_the_receiving_sequence() => _handled.ShouldBeFalse();
    [Fact] void should_leave_default_conversion_available() => _mapped.ShouldBeNull();
}
