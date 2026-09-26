// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Authorization;
using Cratis.Arc.Queries;
using Cratis.Arc.Testing.Queries;
using Microsoft.Extensions.DependencyInjection;

namespace Cratis.Arc.Testing.for_QueryScenario;

public class when_a_lazy_result_owns_a_scope : Specification
{
    readonly QueryScenario<ScenarioReadModel> _scenario = new();
    TrackedQueryScope _scope = default!;
    QueryResult _result = default!;

    void Establish()
    {
        _scenario.Services.AddSingleton<IAuthorizationPolicyRuntime>(new ChangingPrincipalRuntime());
        _scenario.Services.AddScoped(_ => _scope = new TrackedQueryScope());
    }

    async Task Because() => _result = await _scenario.Perform(nameof(ScenarioReadModel.Lazy));

    [Fact] void should_materialize_before_disposing_the_scope() => ((IEnumerable<ScenarioReadModel>)_result.Data).Single().Name.ShouldEqual("In scope");
    [Fact] void should_release_the_scope() => _scope.Disposed.ShouldBeTrue();

    void Destroy() => _scenario.Dispose();
}
