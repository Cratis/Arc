// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Chronicle.Occurrences.for_OccurrenceCommandExecutor;

public class when_the_command_appended_immediately : given.an_occurrence_command_executor
{
    void Establish()
    {
        _duringExecution = () =>
        {
            OccurrenceExecution.Current!.RecordImmediateAppend();
            return Task.CompletedTask;
        };
        CreateExecutor();
    }

    async Task Because() => _exception = await Catch.Exception(() => _executor.Execute(_command, _occurrence, "send-reminder"));

    [Fact] void should_fail_loudly() => _exception.ShouldBeOfExactType<OccurrenceCommandAppendedImmediately>();
    [Fact] async Task should_not_record_a_receipt() => (await StoredReceipt).ShouldBeNull();
}
