// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.ProxyGenerator.Scenarios.Infrastructure;

namespace Cratis.Arc.ProxyGenerator.Scenarios.for_Queries.ModelBound;

[Collection(ScenarioCollectionDefinition.Name)]

public class when_performing_query_for_complex_data : given.a_scenario_web_application
{
    QueryExecutionResult<ComplexReadModel>? _executionResult;
    Guid _testId;

    void Establish()
    {
        _testId = Guid.NewGuid();
        LoadQueryProxy<ComplexReadModel>("GetComplex");
    }

    async Task Because()
    {
        var parameters = new Dictionary<string, object>
        {
            ["id"] = _testId
        };

        _executionResult = await Bridge.PerformQueryViaProxyAsync<ComplexReadModel>("GetComplex", parameters);
    }

    [Fact] void should_return_successful_result() => _executionResult.Result.IsSuccess.ShouldBeTrue();
    [Fact] void should_have_data() => _executionResult.Result.Data.ShouldNotBeNull();
    [Fact]
    void should_have_nested_data()
    {
        var complexData = System.Text.Json.JsonSerializer.Deserialize<ComplexReadModel>(_executionResult.Result.Data.ToString(), Json.Globals.JsonSerializerOptions);
        complexData.ShouldNotBeNull();
        complexData.NestedData.ShouldNotBeNull();
    }
    [Fact]
    void should_have_correct_duration()
    {
        var complexData = System.Text.Json.JsonSerializer.Deserialize<ComplexReadModel>(_executionResult.Result.Data.ToString(), Json.Globals.JsonSerializerOptions);
        complexData.NestedData.Duration.ShouldEqual(TimeSpan.FromHours(2.5));
    }
}
