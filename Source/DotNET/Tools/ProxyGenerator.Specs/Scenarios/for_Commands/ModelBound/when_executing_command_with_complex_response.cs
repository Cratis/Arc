// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Commands;

namespace Cratis.Arc.ProxyGenerator.Scenarios.for_Commands.ModelBound;

[Collection(ScenarioCollectionDefinition.Name)]

public class when_executing_command_with_complex_response : given.a_scenario_web_application
{
    CommandResult<CommandResultWithNestedTypes>? _result;

    void Establish() => LoadCommandProxy<CommandWithComplexResponse>();

    async Task Because()
    {
        var executionResult = await Bridge.ExecuteCommandViaProxyAsync<CommandResultWithNestedTypes>(new CommandWithComplexResponse
        {
            Input = "TestInput"
        });
        _result = executionResult.Result;
    }

    [Fact] void should_return_successful_result() => _result.IsSuccess.ShouldBeTrue();
    [Fact] void should_have_response() => _result.Response.ShouldNotBeNull();
    [Fact] void should_have_message() => _result.Response.Message.ShouldContain("Processed: TestInput");
    [Fact] void should_have_details() => _result.Response.Details.ShouldNotBeNull();
    [Fact] void should_have_metadata_in_details() => _result.Response.Details.Metadata.ShouldNotBeNull();
}
