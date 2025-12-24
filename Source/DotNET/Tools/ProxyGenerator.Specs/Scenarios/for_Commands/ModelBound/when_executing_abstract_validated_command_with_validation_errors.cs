// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Commands;

namespace Cratis.Arc.ProxyGenerator.Scenarios.for_Commands.ModelBound;

public class when_executing_abstract_validated_command_with_validation_errors : given.a_scenario_web_application
{
    CommandResult<object>? _result;
    int _serverCallCount;

    void Establish()
    {
        LoadCommandProxy<AbstractValidatedCommand>();
        _serverCallCount = 0;

        AbstractValidatedCommand.OnHandle = () =>
        {
            _serverCallCount++;
        };
    }

    async Task Because()
    {
        var executionResult = await Bridge.ExecuteCommandViaProxyAsync<object>(new AbstractValidatedCommand
        {
            Username = "ab",
            Password = "short"
        });
        _result = executionResult.Result;
    }

    [Fact] void should_not_be_successful() => _result.IsSuccess.ShouldBeFalse();
    [Fact] void should_not_be_valid() => _result.IsValid.ShouldBeFalse();
    [Fact] void should_have_validation_results() => _result.ValidationResults.ShouldNotBeEmpty();
    [Fact] void should_have_username_validation_error() => _result.ValidationResults.ShouldContain(v => v.Members.Contains("username"));
    [Fact] void should_have_password_validation_error() => _result.ValidationResults.ShouldContain(v => v.Members.Contains("password"));
    [Fact] void should_not_roundtrip_to_server() => _serverCallCount.ShouldEqual(0);
}
