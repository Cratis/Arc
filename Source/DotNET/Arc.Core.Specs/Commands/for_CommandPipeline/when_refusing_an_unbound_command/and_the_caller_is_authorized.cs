// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Commands.for_CommandPipeline.when_refusing_an_unbound_command;

public class and_the_caller_is_authorized : given.an_unbound_command
{
    async Task Because() => _result = await _commandPipeline.RefuseUnboundHosted(typeof(RoleCommand), _bindingFailure, _serviceProvider, false, CancellationToken.None);

    [Fact] void should_report_the_binding_failure() => _result.ValidationResults.ShouldEqual(_bindingFailure.ValidationResults);
    [Fact] void should_keep_the_correlation_id() => _result.CorrelationId.ShouldEqual(_correlationId);
    [Fact] void should_not_build_context_values() => _commandContextValuesBuilder.DidNotReceive().Build(Arg.Any<object>());
    [Fact] void should_not_begin_execution_scopes() => _executionScope.DidNotReceive().Begin(Arg.Any<CommandContext>());
    [Fact] void should_not_run_command_filters() => _commandFilters.DidNotReceive().OnExecution(Arg.Any<CommandContext>());
}
