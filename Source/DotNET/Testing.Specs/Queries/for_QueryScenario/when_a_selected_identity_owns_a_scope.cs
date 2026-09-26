// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Authorization;
using Cratis.Arc.Queries;
using Cratis.Arc.Testing.Queries;
using Microsoft.Extensions.DependencyInjection;

namespace Cratis.Arc.Testing.for_QueryScenario;

public class when_a_selected_identity_owns_a_scope : Specification
{
    readonly QueryScenario<ScenarioReadModel> _scenario = new();
    TrackedQueryScope _scope = default!;
    QueryResult _result = default!;

    void Establish()
    {
        _scenario.Services.AddSingleton<IAuthorizationPolicyRuntime>(new ChangingPrincipalRuntime());
        _scenario.Services.AddScoped(_ => _scope = new TrackedQueryScope());
    }

    async Task Because() => _result = await _scenario.Perform(nameof(ScenarioReadModel.Scoped));

    [Fact] void should_execute_in_the_owned_scope() => ((ScenarioReadModel)_result.Data).Name.ShouldEqual("In scope");
    [Fact] void should_release_the_owned_scope_before_returning() => _scope.Disposed.ShouldBeTrue();

    void Destroy() => _scenario.Dispose();
}
