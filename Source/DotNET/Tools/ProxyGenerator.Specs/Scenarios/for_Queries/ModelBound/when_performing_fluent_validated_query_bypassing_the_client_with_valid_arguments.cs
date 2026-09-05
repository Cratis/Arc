// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Net;
using Cratis.Arc.Queries;

namespace Cratis.Arc.ProxyGenerator.Scenarios.for_Queries.ModelBound;

/// <summary>
/// The counterpart to rejecting invalid arguments: enforcement must not stand in the way of a legitimate request.
/// Without this, a validator that rejected everything would look correct.
/// </summary>
[Collection(ScenarioCollectionDefinition.Name)]
public class when_performing_fluent_validated_query_bypassing_the_client_with_valid_arguments : given.a_scenario_web_application
{
    QueryResult? _result;
    HttpStatusCode _statusCode;

    void Establish()
    {
        LoadQueryProxy<ServerEnforcedValidationReadModel>("SearchByEmailAndMinimumAge");
        ServerEnforcedValidationReadModel.SearchCallCount = 0;
    }

    async Task Because()
    {
        var route = Runtime!.Evaluate<string>("new SearchByEmailAndMinimumAge().route")!;

        var response = await HttpClient!.GetAsync($"{route}?email=author@cratis.io&minAge=21");
        _statusCode = response.StatusCode;
        var json = await response.Content.ReadAsStringAsync();
        _result = System.Text.Json.JsonSerializer.Deserialize<QueryResult>(json, Json.Globals.JsonSerializerOptions);
    }

    [Fact] void should_accept_the_request() => _statusCode.ShouldEqual(HttpStatusCode.OK);
    [Fact] void should_be_valid() => _result.IsValid.ShouldBeTrue();
    [Fact] void should_have_no_validation_results() => _result.ValidationResults.ShouldBeEmpty();
    [Fact] void should_reach_the_query() => ServerEnforcedValidationReadModel.SearchCallCount.ShouldEqual(1);
}
