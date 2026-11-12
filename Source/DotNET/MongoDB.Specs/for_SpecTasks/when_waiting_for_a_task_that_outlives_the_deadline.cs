// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.MongoDB.for_SpecTasks;

public class when_waiting_for_a_task_that_outlives_the_deadline : Specification
{
    bool _completed;

    async Task Because() =>
        _completed = await SpecTasks.CompletesWithin(new TaskCompletionSource().Task, TimeSpan.FromMilliseconds(50));

    [Fact] void should_report_the_wait_as_timed_out() => _completed.ShouldBeFalse();
}
