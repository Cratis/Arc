// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Commands;
using Cratis.Chronicle.Transactions;

namespace Cratis.Arc.Chronicle.Commands.for_TransactionalCommandScope.when_executing_for_an_occurrence;

public class and_the_command_appended_immediately : given.an_occurrence_execution
{
    async Task Because()
    {
        BeginExecution();
        _scope.Begin(_context);
        _exception = await Catch.Exception(() => _store.EventLog.Append("immediate-source", new ScopedEffectHappened(1)));
        AddEvent();
        _result = CommandResult.Error(_correlationId, _exception);
        try
        {
            await _scope.Complete(_context, _result);
        }
        finally
        {
            _scope.End(_context);
        }
    }

    [Fact] void should_refuse_the_immediate_append() => _exception.ShouldBeOfExactType<ImmediateAppendRefused>();
    [Fact] async Task should_not_append_any_events() => (await _store.EventLog.GetFromSequenceNumber(0)).ShouldBeEmpty();
    [Fact] void should_fail_the_command() => _result.IsSuccess.ShouldBeFalse();
    [Fact] void should_not_report_a_receipt() => _execution.Receipt.ShouldBeNull();
}
