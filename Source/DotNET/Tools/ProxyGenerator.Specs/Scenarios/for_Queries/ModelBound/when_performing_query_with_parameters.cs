// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.ProxyGenerator.Scenarios.Infrastructure;

namespace Cratis.Arc.ProxyGenerator.Scenarios.for_Queries.ModelBound;

public class when_performing_query_with_parameters : given.a_scenario_web_application
{
    QueryExecutionResult<IEnumerable<ParameterizedReadModel>>? _executionResult;

    void Establish() => LoadQueryProxy<ParameterizedReadModel>("Search", "/tmp/Search.ts");

    async Task Because()
    {
        var parameters = new Dictionary<string, object>
        {
            ["name"] = "TestName",
            ["category"] = "TestCategory"
        };

        _executionResult = await Bridge.PerformQueryViaProxyAsync<IEnumerable<ParameterizedReadModel>>("Search", parameters);
    }

    [Fact] void should_return_successful_result() => _executionResult.Result.IsSuccess.ShouldBeTrue();
    [Fact] void should_be_authorized() => _executionResult.Result.IsAuthorized.ShouldBeTrue();
    [Fact] void should_have_data() => _executionResult.Result.Data.ShouldNotBeNull();
}
