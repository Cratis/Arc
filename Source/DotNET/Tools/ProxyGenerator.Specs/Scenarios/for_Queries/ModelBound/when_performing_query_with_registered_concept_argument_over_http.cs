// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Net;
using System.Text.Json;
using Cratis.Arc.Queries.ModelBound;
using Cratis.Concepts;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.DependencyInjection;

namespace Cratis.Arc.ProxyGenerator.Scenarios.for_Queries.ModelBound;

[Collection(ScenarioCollectionDefinition.Name)]
public class when_performing_query_with_registered_concept_argument_over_http : given.a_scenario_web_application
{
    HttpStatusCode _statusCode;
    decimal _returnedRate;
    bool _containerCanResolveConcept;

    protected override bool RequiresJavaScriptRuntime => false;

    void Establish() => _containerCanResolveConcept = Host!.Services.GetRequiredService<IServiceProviderIsService>().IsService(typeof(HttpConceptRate));

    async Task Because()
    {
        using var response = await HttpClient!.GetAsync("/api/rate-lookup?RATE=12.5");
        _statusCode = response.StatusCode;
        using var json = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        _returnedRate = json.RootElement.GetProperty("data").GetProperty("value").GetDecimal();
    }

    [Fact] void should_have_the_concept_registered_as_a_service() => _containerCanResolveConcept.ShouldBeTrue();
    [Fact] void should_return_http_200() => _statusCode.ShouldEqual(HttpStatusCode.OK);
    [Fact] void should_return_the_rate_from_the_request() => _returnedRate.ShouldEqual(12.5m);
}

public record HttpConceptRate(decimal Value) : ConceptAs<decimal>(Value);

[ReadModel]
public record HttpConceptRateLookup(decimal Value)
{
    [AllowAnonymous]
    [Path("/api/rate-lookup")]
    public static HttpConceptRateLookup ByRate(HttpConceptRate rate) => new(rate.Value);
}
