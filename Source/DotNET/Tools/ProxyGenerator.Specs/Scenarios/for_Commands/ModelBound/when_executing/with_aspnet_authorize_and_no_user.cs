// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Commands;

namespace Cratis.Arc.ProxyGenerator.Scenarios.for_Commands.ModelBound.when_executing;

/// <summary>
/// <see cref="AuthorizedCommand"/> is marked with Microsoft.AspNetCore.Authorization's [Authorize], which Arc does not
/// enforce on a model-bound command: it never places the artifact's attributes on the endpoint, and its evaluators
/// read only Cratis.Arc.Authorization's attributes. A caller with no identity at all is therefore admitted.
/// </summary>
/// <remarks>
/// Pinned, not endorsed - see https://github.com/Cratis/Arc/issues/2719. Analyzer ARC0020 reports the attribute at
/// build time. When the issue is resolved this spec should flip to match <see cref="with_arc_authorize_and_no_user"/>.
/// </remarks>
[Collection(ScenarioCollectionDefinition.Name)]
public class with_aspnet_authorize_and_no_user : given.a_scenario_web_application
{
    CommandResult<object>? _result;

    void Establish() => LoadCommandProxy<AuthorizedCommand>();

    async Task Because()
    {
        var executionResult = await Bridge.ExecuteCommandViaProxyAsync<object>(new AuthorizedCommand { SecureData = "secret" });
        _result = executionResult.Result;
    }

    [Fact] void should_admit_the_anonymous_caller() => _result.IsAuthorized.ShouldBeTrue();
}
