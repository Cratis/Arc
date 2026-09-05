// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Net;
using Cratis.Arc.Queries;

namespace Cratis.Arc.ProxyGenerator.Scenarios.for_Queries.ModelBound;

/// <summary>
/// The generated proxy rejects these arguments in the browser. Nothing forces a caller to go through the proxy, so
/// the same validator has to reject them at the endpoint — otherwise the rules are decoration rather than
/// enforcement. The route is read off the generated proxy so this exercises exactly the endpoint the client calls.
/// <para>
/// The route is asserted as well as the rejection: a route resolving to a different query would otherwise look like
/// a missing validation failure rather than the spec calling the wrong endpoint.
/// </para>
/// </summary>
[Collection(ScenarioCollectionDefinition.Name)]
public class when_performing_fluent_validated_query_bypassing_the_client : given.a_scenario_web_application
{
    QueryResult? _result;
    HttpStatusCode _statusCode;
    string _route = string.Empty;

    void Establish()
    {
        LoadQueryProxy<ServerEnforcedValidationReadModel>("SearchByEmailAndMinimumAge");
        ServerEnforcedValidationReadModel.SearchCallCount = 0;
    }

    async Task Because()
    {
        _route = Runtime!.Evaluate<string>("new SearchByEmailAndMinimumAge().route")!;

        var response = await HttpClient!.GetAsync($"{_route}?email=invalid-email&minAge=-5");
        _statusCode = response.StatusCode;
        var json = await response.Content.ReadAsStringAsync();
        _result = System.Text.Json.JsonSerializer.Deserialize<QueryResult>(json, Json.Globals.JsonSerializerOptions);
    }

    [Fact] void should_resolve_the_route_for_this_query() => _route.ShouldContain("search-by-email-and-minimum-age");
    [Fact] void should_reject_the_request() => _statusCode.ShouldEqual(HttpStatusCode.BadRequest);
    [Fact] void should_not_be_valid() => _result.IsValid.ShouldBeFalse();
    [Fact] void should_report_the_email_rule() => _result.ValidationResults.ShouldContain(_ => _.Message == SearchByEmailAndMinimumAgeValidator.EmailMessage);
    [Fact] void should_report_the_age_rule() => _result.ValidationResults.ShouldContain(_ => _.Message == SearchByEmailAndMinimumAgeValidator.AgeMessage);
    [Fact] void should_not_reach_the_query() => ServerEnforcedValidationReadModel.SearchCallCount.ShouldEqual(0);
}
