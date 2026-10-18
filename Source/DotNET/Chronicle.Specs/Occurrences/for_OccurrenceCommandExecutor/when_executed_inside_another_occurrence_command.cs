// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Chronicle.Occurrences.for_OccurrenceCommandExecutor;

public class when_executed_inside_another_occurrence_command : given.an_occurrence_command_executor
{
    void Establish() => CreateExecutor();

    async Task Because()
    {
        using var _ = OccurrenceExecution.Begin(new OccurrenceExecution(_key, "Outer"));
        _exception = await Catch.Exception(() => _executor.Execute(_command, _occurrence, "send-reminder"));
    }

    [Fact] void should_refuse() => _exception.ShouldBeOfExactType<OccurrenceCommandCannotRunInsideCommand>();
    [Fact] void should_not_run_the_command() => _pipelineCalls.ShouldEqual(0);
}
