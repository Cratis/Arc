// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Commands.for_CommandPipeline.when_refusing_an_unbound_command;

public class and_it_is_validate_only : given.an_unbound_command
{
    async Task Because() => _result = await _commandPipeline.RefuseUnboundHosted(typeof(RoleCommand), _bindingFailure, _serviceProvider, true, CancellationToken.None);

    [Fact] void should_report_the_binding_failure() => _result.ValidationResults.ShouldEqual(_bindingFailure.ValidationResults);
    [Fact] void should_keep_the_correlation_id() => _result.CorrelationId.ShouldEqual(_correlationId);
    [Fact] void should_not_run_command_filters() => _commandFilters.DidNotReceive().OnExecution(Arg.Any<CommandContext>());
}
