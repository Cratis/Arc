// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Commands;
using Microsoft.Extensions.DependencyInjection;

namespace Cratis.Arc.Testing.Commands.for_CommandScenario.operations;

public class when_an_operation_ignores_a_nested_command_failure : Specification
{
    CommandScenario<given.AttemptNestedOperationBatch> _scenario;
    given.INestedOperationProbe _probe;
    CommandResult _result;

    void Establish()
    {
        _probe = Substitute.For<given.INestedOperationProbe>();
        _scenario = new();
        _scenario.Services.AddSingleton(_probe);
    }

    async Task Because() => _result = await _scenario.Execute(new given.AttemptNestedOperationBatch());

    async Task Destroy() => await _scenario.DisposeAsync();

    [Fact] void should_reject_the_outer_command() => _result.ShouldNotBeSuccessful();

    [Fact] void should_not_execute_the_child() => _probe.DidNotReceive().Child();

    [Fact] void should_not_start_the_following_operation() => _probe.DidNotReceive().Following();

    [Fact] void should_compensate_the_started_operation() => _probe.Received(1).Compensated();

    [Fact] void should_record_only_the_started_operation() => _scenario.Operations.Count.ShouldEqual(1);

    [Fact] void should_preserve_the_observed_method_completion() =>
        _scenario.ShouldHaveExecutedOperation<given.AttemptNestedCommand>();
}
