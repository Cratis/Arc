// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Commands;
using Cratis.Chronicle.Occurrences;

namespace Cratis.Arc.Chronicle.Commands.for_TransactionalCommandScope.when_executing_for_an_occurrence;

public class and_the_command_appended_events : given.an_occurrence_execution
{
    async Task Because()
    {
        BeginExecution();
        _scope.Begin(_context);
        AddEvent();
        _result = CommandResult.Success(_correlationId);
        try
        {
            await _scope.Complete(_context, _result);
        }
        finally
        {
            _scope.End(_context);
        }
    }

    [Fact] void should_succeed() => _result.IsSuccess.ShouldBeTrue();
    [Fact] void should_have_seen_events() => _execution.HadEvents.ShouldBeTrue();
    [Fact] void should_enroll_the_receipt() => _execution.Enrolled.ShouldBeTrue();
    [Fact] void should_report_the_receipt_recorded_with_the_commit() => _execution.Receipt!.Status.ShouldEqual(EffectReceiptStatus.Recorded);
    [Fact] void should_report_an_appended_outcome() => _execution.Receipt!.Receipt.Outcome.ShouldBeOfExactType<Appended>();
    [Fact] async Task should_make_the_receipt_durable() => (await _store.Occurrences.GetEffectReceipt(_key)).ShouldNotBeNull();
}
