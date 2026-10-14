// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Chronicle.Testing.Commands;

namespace Cratis.Arc.Chronicle.Commands.for_CommandScenario.when_appending_concurrently;

public class after_initialization_without_a_queue : given.a_concurrent_append_scenario
{
    Exception _error;

    async Task Establish() => await _scenario.Validate(new AppendWithExplicitScope(_source));
    void Because() => _error = Catch.Exception(() => _scenario.AppendConcurrently(_source, new FactRecorded("competitor")));

    [Fact] void should_refuse_reconfiguring_initialized_services() => _error.ShouldBeOfExactType<CompetingEventsMustBeQueuedBeforeExecution>();
}
