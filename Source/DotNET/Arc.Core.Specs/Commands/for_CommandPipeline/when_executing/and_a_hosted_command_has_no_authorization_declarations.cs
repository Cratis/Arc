// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Authorization;

namespace Cratis.Arc.Commands.for_CommandPipeline.when_executing;

public class and_a_hosted_command_has_no_authorization_declarations : given.a_command_pipeline
{
    CommandResult? _result;
    Exception? _error;

    void Establish() => _serviceProvider.GetService(typeof(AuthorizationDeclarations)).Returns((object?)null);

    async Task Because() => _error = await Catch.Exception(async () =>
        _result = await _commandPipeline.ExecuteHosted(new ProtectedCommand(), _serviceProvider, null, CancellationToken.None));

    [Fact] void should_not_throw_an_unhandled_configuration_error() => _error.ShouldBeNull();
    [Fact] void should_report_unauthorized() => _result?.IsAuthorized.ShouldBeFalse();

    [Authorize(Policy = "Allow")]
    public record ProtectedCommand;
}
