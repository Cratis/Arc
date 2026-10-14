// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Commands;
using Cratis.Chronicle.EventSequences;
using Cratis.Chronicle.EventSequences.Concurrency;
using Cratis.Chronicle.Transactions;

namespace Cratis.Arc.Chronicle.Commands.for_FreshIdentifierEnrollmentConflicts.given;

public class a_recorded_guard : for_FreshIdentifierEnrollmentContributor.given.a_contributor
{
    protected FreshIdentifierEnrollmentConflicts _mapper;
    protected Exception _conflict;
    protected bool _handled;
    protected CommandResult? _mapped;

    void Establish()
    {
        using var registration = _receivingUnit.AddEnrollmentContributor(_contributor);
        _receivingUnit.AddEvents(EventSequenceId.Log, [new(_id, new Created("first"))], []);
        _mapper = new();
        _conflict = new ConflictingEnrollmentScope(_id, ConcurrencyScope.None, new(Cratis.Chronicle.Events.EventSequenceNumber.BeforeFirst, EventSourceId: _id), EventSequenceId.Log);
    }
}
