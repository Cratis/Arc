// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Authorization;

namespace Cratis.Arc.Commands.for_CommandPipeline.when_refusing_an_unbound_command;

public class and_authorization_declarations_are_missing : given.an_unbound_command
{
    void Establish() => _serviceProvider.GetService(typeof(AuthorizationDeclarations)).Returns((object?)null);

    async Task Because() => _result = await _commandPipeline.RefuseUnboundHosted(typeof(RoleCommand), _bindingFailure, _serviceProvider, false, CancellationToken.None);

    [Fact] void should_fail_authorization_configuration() => _result.AuthorizationConfigurationFailed.ShouldBeTrue();
    [Fact] void should_not_report_validation() => _result.ValidationResults.ShouldBeEmpty();
}
