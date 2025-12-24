// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Commands;

namespace Cratis.Arc.ProxyGenerator.Scenarios.for_Commands.ModelBound;

public class when_executing_command_with_validation_errors_produced_by_the_client : given.a_scenario_web_application
{
    CommandResult<object>? _result;
    int _serverCallCount;

    void Establish()
    {
        LoadCommandProxy<FluentValidatedCommand>();
        _serverCallCount = 0;

        FluentValidatedCommand.OnHandle = () => _serverCallCount++;
    }

    async Task Because()
    {
        var executionResult = await Bridge.ExecuteCommandViaProxyAsync<object>(new FluentValidatedCommand
        {
            Name = string.Empty,
            Age = 15,
            Email = "not-an-email"
        });
        _result = executionResult.Result;
    }

    [Fact] void should_not_be_successful() => _result.IsSuccess.ShouldBeFalse();
    [Fact] void should_not_be_valid() => _result.IsValid.ShouldBeFalse();
    [Fact] void should_have_validation_results() => _result.ValidationResults.ShouldNotBeEmpty();
    [Fact] void should_have_name_validation_error() => _result.ValidationResults.ShouldContain(v => v.Members.Contains("name"));
    [Fact] void should_have_age_validation_error() => _result.ValidationResults.ShouldContain(v => v.Members.Contains("age"));
    [Fact] void should_have_email_validation_error() => _result.ValidationResults.ShouldContain(v => v.Members.Contains("email"));
    [Fact] void should_not_roundtrip_to_server() => _serverCallCount.ShouldEqual(0);
}
