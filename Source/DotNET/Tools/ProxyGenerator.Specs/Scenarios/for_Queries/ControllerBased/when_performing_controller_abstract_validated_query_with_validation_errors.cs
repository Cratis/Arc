// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Queries;

namespace Cratis.Arc.ProxyGenerator.Scenarios.for_Queries.ControllerBased;

public class when_performing_controller_abstract_validated_query_with_validation_errors : given.a_scenario_web_application
{
    QueryResult? _result;

    void Establish() => ControllerQueriesController.AbstractValidatedCallCount = 0;

    async Task Because()
    {
        var response = await HttpClient.GetAsync("/api/controller-queries/abstract-validated?code=AB&minAmount=-100");
        var json = await response.Content.ReadAsStringAsync();
        _result = System.Text.Json.JsonSerializer.Deserialize<QueryResult>(json, Json.Globals.JsonSerializerOptions);
    }

    [Fact] void should_not_be_successful() => _result.IsSuccess.ShouldBeFalse();
    [Fact] void should_not_be_valid() => _result.IsValid.ShouldBeFalse();
    [Fact] void should_have_validation_results() => _result.ValidationResults.ShouldNotBeEmpty();
    [Fact] void should_not_roundtrip_to_server() => ControllerQueriesController.AbstractValidatedCallCount.ShouldEqual(0);
}
