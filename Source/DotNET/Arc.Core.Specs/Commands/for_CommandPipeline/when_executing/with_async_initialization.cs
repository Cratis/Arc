// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Commands.for_CommandPipeline.when_executing;

public class with_async_initialization : given.a_pipeline_with_async_scopes
{
    async Task Because() => _result = await _commandPipeline.Execute(_command, _serviceProvider);

    [Fact] void should_initialize_after_all_sync_begins_and_before_filters() => _calls.ShouldEqual(["first begin", "sync begin", "second begin", "first async", "second async", "filters", "second complete", "sync complete", "first complete"]);
    [Fact] void should_succeed() => _result.IsSuccess.ShouldBeTrue();
}
