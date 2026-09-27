// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Authorization;
using Microsoft.Extensions.Logging;

namespace Cratis.Arc.Commands.for_CommandPipeline.when_validating;

public class and_a_hosted_command_has_no_authorization_declarations : given.a_command_pipeline
{
    CommandResult? _result;
    Exception? _error;
    ILogger<CommandPipeline> _logger;

    void Establish()
    {
        _serviceProvider.GetService(typeof(AuthorizationDeclarations)).Returns((object?)null);
        _logger = Substitute.For<ILogger<CommandPipeline>>();
        _logger.IsEnabled(Arg.Any<LogLevel>()).Returns(true);
        _serviceProvider.GetService(typeof(ILogger<CommandPipeline>)).Returns(_logger);
    }

    async Task Because() => _error = await Catch.Exception(async () =>
        _result = await _commandPipeline.ValidateHosted(new ProtectedCommand(), _serviceProvider, null, CancellationToken.None));

    [Fact] void should_not_throw_an_unhandled_configuration_error() => _error.ShouldBeNull();
    [Fact] void should_return_a_result() => _result.ShouldNotBeNull();
    [Fact] void should_report_unauthorized() => _result!.IsAuthorized.ShouldBeFalse();
    [Fact] void should_log_the_configuration_failure() => _logger.ReceivedCalls()
        .Any(call => call.GetMethodInfo().Name == nameof(ILogger.Log) &&
            call.GetArguments()[0] is LogLevel.Error &&
            call.GetArguments()[3] is InvalidAuthorizationConfiguration)
        .ShouldBeTrue();

    [Authorize(Policy = "Allow")]
    public record ProtectedCommand;
}
