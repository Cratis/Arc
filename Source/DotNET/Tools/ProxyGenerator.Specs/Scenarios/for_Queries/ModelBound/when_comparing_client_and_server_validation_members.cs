// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.ProxyGenerator.Scenarios.Infrastructure;
using Cratis.Arc.Queries;

namespace Cratis.Arc.ProxyGenerator.Scenarios.for_Queries.ModelBound;

/// <summary>
/// A form matches a validation failure to the field that caused it by member name, so if the two sides name members
/// differently a server rejection highlights nothing. This runs the generated proxy and the endpoint against the same
/// arguments and compares what each reports, rather than asserting one side against an assumption about the other —
/// a spec that pinned only the server would still pass if the client's convention changed underneath it.
/// </summary>
[Collection(ScenarioCollectionDefinition.Name)]
public class when_comparing_client_and_server_validation_members : given.a_scenario_web_application
{
    QueryExecutionResult<IEnumerable<ServerEnforcedValidationReadModel>>? _clientResult;
    QueryResult? _serverResult;

    void Establish()
    {
        LoadQueryProxy<ServerEnforcedValidationReadModel>("SearchByEmailAndMinimumAge");
        ServerEnforcedValidationReadModel.SearchCallCount = 0;
    }

    async Task Because()
    {
        var parameters = new Dictionary<string, object>
        {
            ["email"] = "invalid-email",
            ["minAge"] = -5
        };

        _clientResult = await Bridge!.PerformQueryViaProxyAsync<IEnumerable<ServerEnforcedValidationReadModel>>(
            "SearchByEmailAndMinimumAge",
            parameters);

        var route = Runtime!.Evaluate<string>("new SearchByEmailAndMinimumAge().route")!;
        var response = await HttpClient!.GetAsync($"{route}?email=invalid-email&minAge=-5");
        var json = await response.Content.ReadAsStringAsync();
        _serverResult = System.Text.Json.JsonSerializer.Deserialize<QueryResult>(json, Json.Globals.JsonSerializerOptions);
    }

    [Fact] void should_be_rejected_by_the_client() => _clientResult.Result.IsValid.ShouldBeFalse();
    [Fact] void should_be_rejected_by_the_server() => _serverResult.IsValid.ShouldBeFalse();

    [Fact] void should_report_the_same_members_on_both_sides() =>
        _serverResult.ValidationResults.SelectMany(_ => _.Members).Distinct().Order().ShouldContainOnly(
            _clientResult.Result.ValidationResults.SelectMany(_ => _.Members).Distinct().Order());
}
