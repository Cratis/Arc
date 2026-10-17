// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Chronicle.Occurrences.for_OccurrenceCommandExecutor;

public class when_executing_with_explicit_roles : given.an_occurrence_command_executor
{
    void Establish()
    {
        CreateExecutor();
    }

    async Task Because() => _result = await _executor.Execute(_command, _occurrence, "send-reminder", ["Scheduler"]);

    [Fact] void should_run_as_the_system_with_exactly_those_roles() => _systemExecution.Received(1).AsSystem(Arg.Is<string[]>(_ => _.Length == 1 && _[0] == "Scheduler"));
}
