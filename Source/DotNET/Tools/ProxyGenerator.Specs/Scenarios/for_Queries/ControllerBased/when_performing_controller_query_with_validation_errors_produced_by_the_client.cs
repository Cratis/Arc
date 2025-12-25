// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.ProxyGenerator.Scenarios.Infrastructure;

namespace Cratis.Arc.ProxyGenerator.Scenarios.for_Queries.ControllerBased;

public class when_performing_controller_query_with_validation_errors_produced_by_the_client : given.a_scenario_web_application
{
    QueryExecutionResult<object[]>? _executionResult;

    void Establish()
    {
        LoadControllerQueryProxy<ControllerQueriesController>(nameof(ControllerQueriesController.SearchFluentValidated));
        ControllerQueriesController.FluentValidatedCallCount = 0;
    }

    async Task Because()
    {
        var parameters = new Dictionary<string, object>
        {
            ["email"] = "invalid-email",
            ["minAge"] = 200
        };

        _executionResult = await Bridge.PerformQueryViaProxyAsync<object[]>("SearchFluentValidated", parameters);
    }

    [Fact] void should_not_be_successful() => _executionResult.Result.IsSuccess.ShouldBeFalse();
    [Fact] void should_not_be_valid() => _executionResult.Result.IsValid.ShouldBeFalse();
    [Fact] void should_have_validation_results() => _executionResult.Result.ValidationResults.ShouldNotBeEmpty();
    [Fact] void should_not_roundtrip_to_server() => ControllerQueriesController.FluentValidatedCallCount.ShouldEqual(0);
}
