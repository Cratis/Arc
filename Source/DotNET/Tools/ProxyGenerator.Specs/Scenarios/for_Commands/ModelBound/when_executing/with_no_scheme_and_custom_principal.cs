// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Commands;
using Cratis.Arc.ProxyGenerator.Scenarios.Infrastructure;

namespace Cratis.Arc.ProxyGenerator.Scenarios.for_Commands.ModelBound.when_executing;

[Collection(ScenarioCollectionDefinition.Name)]
public class with_no_scheme_and_custom_principal : given.a_scenario_web_application
{
    CommandResult<object>? _result;

    void Establish()
    {
        LoadCommandProxy<NoSchemePrincipalCommand>();
        HttpClient!.DefaultRequestHeaders.Add("X-Default", "active");
        HttpClient.DefaultRequestHeaders.Add("X-Custom-Principal", "true");
    }

    async Task Because() => _result = (await Bridge!.ExecuteCommandViaProxyAsync<object>(new NoSchemePrincipalCommand())).Result;

    [Fact] void should_authorize_without_switching_scheme() => _result!.IsAuthorized.ShouldBeTrue();
    [Fact] void should_preserve_the_custom_principal_subclass() => NoSchemePrincipalCommand.PrincipalType.ShouldEqual(typeof(ScenarioPrincipal));
    [Fact] void should_not_replace_the_request_principal() => NoSchemePrincipalCommand.SamePrincipal.ShouldBeTrue();
}
