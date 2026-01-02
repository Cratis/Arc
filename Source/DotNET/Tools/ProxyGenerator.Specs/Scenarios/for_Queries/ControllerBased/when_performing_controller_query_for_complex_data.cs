// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Queries;

namespace Cratis.Arc.ProxyGenerator.Scenarios.for_Queries.ControllerBased;

[Collection(ScenarioCollectionDefinition.Name)]

public class when_performing_controller_query_for_complex_data : given.a_scenario_web_application
{
    QueryResult? _result;

    async Task Because()
    {
        var response = await HttpClient.GetAsync("/api/controller-queries/complex");
        var json = await response.Content.ReadAsStringAsync();
        _result = System.Text.Json.JsonSerializer.Deserialize<QueryResult>(json, Json.Globals.JsonSerializerOptions);
    }

    [Fact] void should_return_successful_result() => _result.IsSuccess.ShouldBeTrue();
    [Fact] void should_have_data() => _result.Data.ShouldNotBeNull();
    [Fact]
    void should_deserialize_nested_data()
    {
        var complexItem = System.Text.Json.JsonSerializer.Deserialize<ControllerComplexItem>(_result.Data!.ToString()!, Json.Globals.JsonSerializerOptions);
        complexItem.ShouldNotBeNull();
        complexItem.Nested.ShouldNotBeNull();
    }
    [Fact]
    void should_have_correct_processing_duration()
    {
        var complexItem = System.Text.Json.JsonSerializer.Deserialize<ControllerComplexItem>(_result.Data!.ToString()!, Json.Globals.JsonSerializerOptions);
        complexItem!.Nested!.ProcessingDuration.ShouldEqual(TimeSpan.FromSeconds(45));
    }
}
