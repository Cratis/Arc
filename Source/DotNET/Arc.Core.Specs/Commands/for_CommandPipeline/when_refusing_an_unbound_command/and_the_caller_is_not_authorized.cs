// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Commands.for_CommandPipeline.when_refusing_an_unbound_command;

public class and_the_caller_is_not_authorized : given.an_unbound_command
{
    void Establish() => _evaluator.IsAuthorized(typeof(RoleCommand)).Returns(false);

    async Task Because() => _result = await _commandPipeline.RefuseUnboundHosted(typeof(RoleCommand), _bindingFailure, _serviceProvider, false, CancellationToken.None);

    [Fact] void should_refuse_authorization() => _result.IsAuthorized.ShouldBeFalse();
    [Fact] void should_not_report_the_body_failure() => _result.ValidationResults.ShouldBeEmpty();
}
