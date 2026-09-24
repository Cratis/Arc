// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;
using Cratis.Arc.ProxyGenerator.Scenarios.Infrastructure;

namespace Cratis.Arc.ProxyGenerator.Scenarios.for_Queries.ModelBound;

[Collection(ScenarioCollectionDefinition.Name)]
public class when_policy_scheme_succeeds : given.a_scenario_web_application
{
    QueryExecutionResult<PolicyProtectedReadModel>? _result;
    int _performedBefore;

    void Establish()
    {
        _performedBefore = PolicyProtectedReadModel.Performed;
        LoadQueryProxy<PolicyProtectedReadModel>("All");
        HttpClient!.DefaultRequestHeaders.Add("X-Default", "active");
        HttpClient.DefaultRequestHeaders.Add("X-Special", "active");
    }

    async Task Because() => _result = await Bridge!.PerformQueryViaProxyAsync<PolicyProtectedReadModel>("All");

    [Fact] void should_authorize_the_selected_identity() => _result!.Result.IsAuthorized.ShouldBeTrue();
    [Fact] void should_run_the_query() => PolicyProtectedReadModel.Performed.ShouldEqual(_performedBefore + 1);
    [Fact] void should_return_the_selected_identity() =>
        JsonSerializer.Deserialize<PolicyProtectedReadModel>(((JsonElement)_result!.Result!.Data).GetRawText(), Json.Globals.JsonSerializerOptions)!.Value.ShouldEqual("Special");
    [Fact] void should_use_the_selected_http_identity() => PolicyProtectedReadModel.LastHttpCaller.ShouldEqual("Special");
}
