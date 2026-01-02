// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Commands;

namespace Cratis.Arc.ProxyGenerator.Scenarios.for_Commands.ModelBound;

[Collection(ScenarioCollectionDefinition.Name)]

public class when_executing_command_that_throws_exception : given.a_scenario_web_application
{
    CommandResult<object>? _result;

    void Establish() => LoadCommandProxy<ExceptionCommand>();

    async Task Because()
    {
        var executionResult = await Bridge.ExecuteCommandViaProxyAsync<object>(new ExceptionCommand
        {
            ShouldThrow = true
        });
        _result = executionResult.Result;
    }

    [Fact] void should_not_be_successful() => _result.IsSuccess.ShouldBeFalse();
    [Fact] void should_have_exceptions() => _result.HasExceptions.ShouldBeTrue();
    [Fact] void should_have_exception_messages() => _result.ExceptionMessages.ShouldNotBeEmpty();
}
