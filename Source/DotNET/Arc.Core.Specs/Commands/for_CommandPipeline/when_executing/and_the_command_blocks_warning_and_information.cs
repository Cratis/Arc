// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Validation;

namespace Cratis.Arc.Commands.for_CommandPipeline.when_executing;

public class and_the_command_blocks_warning_and_information : given.a_command_with_validation_policy
{
    CommandResult[] _results;

    async Task Because()
    {
        var results = new List<CommandResult>();
        foreach (var severity in (ValidationResultSeverity[])[ValidationResultSeverity.Warning, ValidationResultSeverity.Information])
        {
            _failureSeverity = severity;
            results.Add(await _commandPipeline.Execute(_command, _serviceProvider));
            results.Add(await _commandPipeline.Execute(_command, _serviceProvider, ValidationResultSeverity.Error));
            results.Add(await _commandPipeline.Execute(_command));
        }
        _results = [.. results];
    }

    [Fact] void should_reject_every_failure() => _results.All(result => !result.IsSuccess).ShouldBeTrue();
    [Fact] void should_keep_the_original_severity_and_message() => _results.Select((result, index) =>
        result.ValidationResults.Single().Message == "Keep this message" &&
        result.ValidationResults.Single().Severity == (index < 3 ? ValidationResultSeverity.Warning : ValidationResultSeverity.Information))
        .All(matches => matches).ShouldBeTrue();
    [Fact] void should_not_invoke_the_handler() => _handler.DidNotReceive().Handle(Arg.Any<CommandContext>());
}
