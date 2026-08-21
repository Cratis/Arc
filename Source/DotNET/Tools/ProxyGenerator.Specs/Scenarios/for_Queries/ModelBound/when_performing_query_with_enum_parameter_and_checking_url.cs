// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.ProxyGenerator.Scenarios.Infrastructure;

namespace Cratis.Arc.ProxyGenerator.Scenarios.for_Queries.ModelBound;

/// <summary>
/// Verifies that an enum query parameter survives proxy generation and reaches the server as a query argument.
/// </summary>
/// <remarks>
/// The defect this pins was not a malformed value but a missing one: an enum parameter was classified as an
/// injected dependency rather than a query argument, so the proxy had no way to send it at all and the server
/// saw the enum's default. Asserting the value reaches the URL is what distinguishes those two outcomes.
/// </remarks>
[Collection(ScenarioCollectionDefinition.Name)]
public class when_performing_query_with_enum_parameter_and_checking_url : given.a_scenario_web_application
{
    QueryExecutionResult<IEnumerable<EnumParameterReadModel>>? _executionResult;

    void Establish() => LoadQueryProxy<EnumParameterReadModel>("SearchByStatus");

    async Task Because()
    {
        var parameters = new Dictionary<string, object>
        {
            ["status"] = (int)ReadModelStatus.Active
        };

        _executionResult = await Bridge.PerformQueryViaProxyAsync<IEnumerable<EnumParameterReadModel>>("SearchByStatus", parameters);
    }

    [Fact] void should_return_successful_result() => _executionResult.Result.IsSuccess.ShouldBeTrue();
    [Fact] void should_include_the_enum_value_in_url() => _executionResult.RequestUrl.ShouldContain($"status={(int)ReadModelStatus.Active}");
    [Fact] void should_pass_the_enum_value_through_to_the_query() => _executionResult.RawJson.ShouldContain($"Item {nameof(ReadModelStatus.Active)}");
}
