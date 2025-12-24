// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Commands;

namespace Cratis.Arc.ProxyGenerator.Scenarios.for_Commands.ModelBound;

public class when_executing_data_annotations_validated_command_with_validation_errors_produced_by_the_client : given.a_scenario_web_application
{
    CommandResult<object>? _result;

    void Establish()
    {
        LoadCommandProxy<ValidatedCommand>();
    }

    async Task Because()
    {
        var executionResult = await Bridge.ExecuteCommandViaProxyAsync<object>(new ValidatedCommand
        {
            Name = string.Empty, // Required violation
            Value = 150, // Range violation (max 100)
            Email = "not-an-email" // Email format violation
        });
        _result = executionResult.Result;
    }

    [Fact] void should_not_be_successful() => _result.IsSuccess.ShouldBeFalse();
    [Fact] void should_not_be_valid() => _result.IsValid.ShouldBeFalse();
    [Fact] void should_have_validation_results() => _result.ValidationResults.ShouldNotBeEmpty();
    [Fact] void should_have_name_validation_error() => _result.ValidationResults.ShouldContain(v => v.Members.Contains("name"));
    [Fact] void should_have_value_validation_error() => _result.ValidationResults.ShouldContain(v => v.Members.Contains("value"));
    [Fact] void should_have_email_validation_error() => _result.ValidationResults.ShouldContain(v => v.Members.Contains("email"));
}
