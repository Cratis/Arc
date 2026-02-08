// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Commands;

namespace Cratis.Arc.ProxyGenerator.Scenarios.for_Commands.ModelBound.when_validating;

[Collection(ScenarioCollectionDefinition.Name)]

public class with_validation_errors : given.a_scenario_web_application
{
    CommandResult<object>? _result;

    void Establish() => LoadCommandProxy<ValidatedCommand>();

    async Task Because()
    {
        var executionResult = await Bridge.ValidateCommandViaProxyAsync<object>(new ValidatedCommand
        {
            Name = string.Empty,
            Value = 150,
            Email = "not-an-email"
        });
        _result = executionResult.Result;
    }

    [Fact] void should_not_be_successful() => _result.IsSuccess.ShouldBeFalse();
    [Fact] void should_not_be_valid() => _result.IsValid.ShouldBeFalse();
    [Fact] void should_have_validation_results() => _result.ValidationResults.ShouldNotBeEmpty();
}
