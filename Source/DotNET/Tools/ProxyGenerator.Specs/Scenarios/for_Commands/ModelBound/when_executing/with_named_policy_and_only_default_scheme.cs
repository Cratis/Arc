// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Commands;

namespace Cratis.Arc.ProxyGenerator.Scenarios.for_Commands.ModelBound.when_executing;

[Collection(ScenarioCollectionDefinition.Name)]
public class with_named_policy_and_only_default_scheme : given.a_scenario_web_application
{
    CommandResult<object>? _result;
    int _handledBefore;

    void Establish()
    {
        _handledBefore = PolicyProtectedCommand.Handled;
        LoadCommandProxy<PolicyProtectedCommand>();
        HttpClient!.DefaultRequestHeaders.Add("X-Default", "active");
    }

    async Task Because() => _result = (await Bridge!.ExecuteCommandViaProxyAsync<object>(new PolicyProtectedCommand())).Result;

    [Fact] void should_deny_the_default_identity() => _result!.IsAuthorized.ShouldBeFalse();
    [Fact] void should_not_call_the_handler() => PolicyProtectedCommand.Handled.ShouldEqual(_handledBefore);
}
