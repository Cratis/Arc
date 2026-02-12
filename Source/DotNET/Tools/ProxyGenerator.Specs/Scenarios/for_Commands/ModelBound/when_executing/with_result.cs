// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Commands;

namespace Cratis.Arc.ProxyGenerator.Scenarios.for_Commands.ModelBound.when_executing;

[Collection(ScenarioCollectionDefinition.Name)]

public class with_result : given.a_scenario_web_application
{
    CommandResult<CommandResultData>? _result;

    void Establish() => LoadCommandProxy<CommandWithResult>();

    async Task Because()
    {
        var executionResult = await Bridge.ExecuteCommandViaProxyAsync<CommandResultData>(new CommandWithResult
        {
            Input = "TestInput"
        });
        _result = executionResult.Result;
    }

    [Fact] void should_return_successful_result() => _result.IsSuccess.ShouldBeTrue();
    [Fact] void should_have_response() => _result.Response.ShouldNotBeNull();
    [Fact] void should_have_processed_value() => _result.Response.ProcessedValue.ShouldContain("Processed: TestInput");
}
