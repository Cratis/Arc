// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.MongoDB.for_SpecTasks;

public class when_waiting_for_a_task_that_faults_with_a_timeout : Specification
{
    bool _completed;

    async Task Because() =>
        _completed = await SpecTasks.CompletesWithin(Task.FromException(new TimeoutException("The operation timed out")), TimeSpan.FromSeconds(5));

    [Fact] void should_report_the_task_as_completed() => _completed.ShouldBeTrue();
}
