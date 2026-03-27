// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Commands;

namespace Cratis.Arc.ProxyGenerator.Scenarios.for_Commands.ModelBound.when_executing;

[Collection(ScenarioCollectionDefinition.Name)]

public class with_guid_response : given.a_scenario_web_application
{
    CommandResult<GuidResponseData>? _result;

    void Establish() => LoadCommandProxy<CommandWithGuidResponse>("/tmp/CommandWithGuidResponse.ts");

    async Task Because()
    {
        var executionResult = await Bridge.ExecuteCommandViaProxyAsync<GuidResponseData>(new CommandWithGuidResponse
        {
            Name = "TestUser",
            Id = Guid.NewGuid()
        });
        _result = executionResult.Result;
    }

    [Fact] void should_return_successful_result() => _result.IsSuccess.ShouldBeTrue();
    [Fact] void should_have_response() => _result.Response.ShouldNotBeNull();
    [Fact] void should_have_correct_name() => _result.Response.Name.ShouldEqual("TestUser");
    [Fact] void should_have_correct_user_id() => _result.Response.UserId.ShouldEqual(new Guid(0x12345678, 0x1234, 0x1234, 0x12, 0x34, 0x12, 0x34, 0x56, 0x78, 0x9a, 0xbc));
    [Fact] void should_have_correct_session_id() => _result.Response.SessionId.ShouldEqual(new Guid(0x87654321, 0x4321, 0x4321, 0x43, 0x21, 0xcb, 0xa9, 0x87, 0x65, 0x43, 0x21));
}
