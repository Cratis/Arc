// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Commands;

namespace Cratis.Arc.ProxyGenerator.Scenarios.for_Commands.ModelBound.when_executing;

/// <summary>
/// <see cref="AuthorizedCommand"/> is marked with ASP.NET Core's <c>[Authorize]</c> rather than Arc's. Hosted on ASP.NET
/// Core, Arc enforces both, so a caller with no identity is rejected exactly as <see cref="with_arc_authorize_and_no_user"/>
/// is. Between v18.2.0 and this fix the ASP.NET Core attribute was read by nothing and this caller was admitted.
/// </summary>
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

    [Fact] void should_not_be_authorized() => _result.IsAuthorized.ShouldBeFalse();
    [Fact] void should_not_succeed() => _result.IsSuccess.ShouldBeFalse();
}
