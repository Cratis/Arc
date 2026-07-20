// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Net;
using Cratis.Arc.Queries;

namespace Cratis.Arc.ProxyGenerator.Scenarios.for_Queries.ModelBound;

/// <summary>
/// The generated proxy rejects these arguments in the browser. Nothing forces a caller to go through the proxy, so
/// the same validator has to reject them at the endpoint — otherwise the rules are decoration rather than enforcement.
/// The route is read off the generated proxy so this exercises exactly the endpoint the client would have called.
/// </summary>
[Collection(ScenarioCollectionDefinition.Name)]
public class when_performing_fluent_validated_query_bypassing_the_client : given.a_scenario_web_application
{
    QueryResult? _result;
    HttpStatusCode _statusCode;

    void Establish()
    {
        LoadQueryProxy<FluentValidatedReadModel>("GetByEmailAndAge");
        FluentValidatedReadModel.GetByEmailCallCount = 0;
    }

    async Task Because()
    {
        var route = Runtime!.Evaluate<string>("new GetByEmailAndAge().route")!;

        var response = await HttpClient!.GetAsync($"{route}?email=invalid-email&minAge=-5");
        _statusCode = response.StatusCode;
        var json = await response.Content.ReadAsStringAsync();
        _result = System.Text.Json.JsonSerializer.Deserialize<QueryResult>(json, Json.Globals.JsonSerializerOptions);
    }

    [Fact] void should_reject_the_request() => _statusCode.ShouldEqual(HttpStatusCode.BadRequest);
    [Fact] void should_not_be_valid() => _result.IsValid.ShouldBeFalse();
    [Fact] void should_report_the_email_rule() => _result.ValidationResults.ShouldContain(_ => _.Message == FluentValidatedReadModelGetByEmailAndAgeValidator.EmailRequiredMessage);
    [Fact] void should_report_the_age_rule() => _result.ValidationResults.ShouldContain(_ => _.Message == FluentValidatedReadModelGetByEmailAndAgeValidator.AgeRangeMessage);
    [Fact] void should_not_reach_the_query() => FluentValidatedReadModel.GetByEmailCallCount.ShouldEqual(0);
}
