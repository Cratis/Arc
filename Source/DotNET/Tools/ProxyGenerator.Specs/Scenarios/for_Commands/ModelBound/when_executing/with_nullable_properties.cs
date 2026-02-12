// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Commands;

namespace Cratis.Arc.ProxyGenerator.Scenarios.for_Commands.ModelBound.when_executing;

[Collection(ScenarioCollectionDefinition.Name)]

public class with_nullable_properties : given.a_scenario_web_application
{
    CommandResult<string>? _result;

    void Establish() => LoadCommandProxy<CommandWithNullableProperties>();

    async Task Because()
    {
        var executionResult = await Bridge.ExecuteCommandViaProxyAsync<string>(new CommandWithNullableProperties
        {
            Name = "TestName"
        });
        _result = executionResult.Result;
    }

    [Fact] void should_return_successful_result() => _result.IsSuccess.ShouldBeTrue();
    [Fact] void should_be_authorized() => _result.IsAuthorized.ShouldBeTrue();
    [Fact] void should_be_valid() => _result.IsValid.ShouldBeTrue();
    [Fact] void should_have_no_exceptions() => _result.HasExceptions.ShouldBeFalse();
    [Fact] void should_have_no_validation_results() => _result.ValidationResults.ShouldBeEmpty();
    [Fact] void should_have_called_handler() => _result.Response.ShouldEqual("Handled: TestName");
}
