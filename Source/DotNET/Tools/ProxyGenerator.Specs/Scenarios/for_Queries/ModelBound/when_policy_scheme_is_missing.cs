// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.ProxyGenerator.Scenarios.Infrastructure;

namespace Cratis.Arc.ProxyGenerator.Scenarios.for_Queries.ModelBound;

[Collection(ScenarioCollectionDefinition.Name)]
public class when_policy_scheme_is_missing : given.a_scenario_web_application
{
    QueryExecutionResult<PolicyProtectedReadModel>? _result;
    int _performedBefore;

    void Establish()
    {
        _performedBefore = PolicyProtectedReadModel.Performed;
        LoadQueryProxy<PolicyProtectedReadModel>("All");
        HttpClient!.DefaultRequestHeaders.Add("X-Default", "active");
    }

    async Task Because() => _result = await Bridge!.PerformQueryViaProxyAsync<PolicyProtectedReadModel>("All");

    [Fact] void should_deny_default_authentication() => _result!.Result.IsAuthorized.ShouldBeFalse();
    [Fact] void should_not_run_the_query() => PolicyProtectedReadModel.Performed.ShouldEqual(_performedBefore);
}
