// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Commands;

namespace Cratis.Arc.ProxyGenerator.Scenarios.for_Commands.ModelBound.when_executing;

[Collection(ScenarioCollectionDefinition.Name)]
public class with_named_policy_and_selected_scheme : given.a_scenario_web_application
{
    CommandResult<object>? _result;
    int _handledBefore;

    void Establish()
    {
        _handledBefore = PolicyProtectedCommand.Handled;
        LoadCommandProxy<PolicyProtectedCommand>();
        HttpClient!.DefaultRequestHeaders.Add("X-Default", "active");
        HttpClient.DefaultRequestHeaders.Add("X-Special", "active");
    }

    async Task Because() => _result = (await Bridge!.ExecuteCommandViaProxyAsync<object>(new PolicyProtectedCommand())).Result;

    [Fact] void should_accept_the_selected_identity() => _result!.IsAuthorized.ShouldBeTrue();
    [Fact] void should_call_the_handler_once() => PolicyProtectedCommand.Handled.ShouldEqual(_handledBefore + 1);
    [Fact] void should_use_the_selected_arc_identity_in_the_handler() => PolicyProtectedCommand.LastArcCaller.ShouldEqual("Special");
    [Fact] void should_use_the_selected_http_identity_in_the_handler() => PolicyProtectedCommand.LastHttpCaller.ShouldEqual("Special");
}
