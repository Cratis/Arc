// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Commands;

namespace Cratis.Arc.ProxyGenerator.Scenarios.for_Commands.ModelBound.when_executing;

[Collection(ScenarioCollectionDefinition.Name)]
public class with_arc_authorize_and_no_user : given.a_scenario_web_application
{
    CommandResult<object>? _result;

    void Establish() => LoadCommandProxy<ArcAuthorizedCommand>();

    async Task Because()
    {
        var executionResult = await Bridge.ExecuteCommandViaProxyAsync<object>(new ArcAuthorizedCommand { SecureData = "secret" });
        _result = executionResult.Result;
    }

    [Fact] void should_not_be_authorized() => _result.IsAuthorized.ShouldBeFalse();
    [Fact] void should_not_succeed() => _result.IsSuccess.ShouldBeFalse();
}
