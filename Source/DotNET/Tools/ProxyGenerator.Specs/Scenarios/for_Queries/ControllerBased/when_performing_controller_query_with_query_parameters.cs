// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Queries;

namespace Cratis.Arc.ProxyGenerator.Scenarios.for_Queries.ControllerBased;

[Collection(ScenarioCollectionDefinition.Name)]

public class when_performing_controller_query_with_query_parameters : given.a_scenario_web_application
{
    QueryResult? _result;

    async Task Because()
    {
        var response = await HttpClient.GetAsync("/api/controller-queries/items/search?name=TestName&minValue=5&maxValue=100");
        var json = await response.Content.ReadAsStringAsync();
        _result = System.Text.Json.JsonSerializer.Deserialize<QueryResult>(json, Json.Globals.JsonSerializerOptions);
    }

    [Fact] void should_return_successful_result() => _result.IsSuccess.ShouldBeTrue();
    [Fact] void should_have_data() => _result.Data.ShouldNotBeNull();
}
