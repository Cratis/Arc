// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Queries;

namespace Cratis.Arc.ProxyGenerator.Scenarios.for_Queries.ControllerBased;

[Collection(ScenarioCollectionDefinition.Name)]

public class when_performing_controller_query_that_throws_exception : given.a_scenario_web_application
{
    QueryResult? _result;

    async Task Because()
    {
        var response = await HttpClient.GetAsync("/api/controller-queries/exception?shouldThrow=true");
        var json = await response.Content.ReadAsStringAsync();
        _result = System.Text.Json.JsonSerializer.Deserialize<QueryResult>(json, Json.Globals.JsonSerializerOptions);
    }

    [Fact] void should_not_be_successful() => _result.IsSuccess.ShouldBeFalse();
    [Fact] void should_have_exceptions() => _result.HasExceptions.ShouldBeTrue();
}
