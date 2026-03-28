// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.ProxyGenerator.Scenarios.Infrastructure;

namespace Cratis.Arc.ProxyGenerator.Scenarios.for_Queries.ControllerBased;

[Collection(ScenarioCollectionDefinition.Name)]

public class when_performing_controller_query_for_single_item : given.a_scenario_web_application
{
    QueryExecutionResult<ControllerQueryItem>? _executionResult;
    Guid _itemId;

    void Establish()
    {
        _itemId = Guid.NewGuid();
        LoadControllerQueryProxy<ControllerQueriesController>("GetById");
    }

    async Task Because()
    {
        var parameters = new Dictionary<string, object>
        {
            ["id"] = _itemId
        };

        _executionResult = await Bridge.PerformQueryViaProxyAsync<ControllerQueryItem>("GetById", parameters);
    }

    [Fact] void should_return_successful_result() => _executionResult.Result.IsSuccess.ShouldBeTrue();
    [Fact] void should_be_authorized() => _executionResult.Result.IsAuthorized.ShouldBeTrue();
    [Fact] void should_be_valid() => _executionResult.Result.IsValid.ShouldBeTrue();
    [Fact] void should_have_data() => _executionResult.Result.Data.ShouldNotBeNull();
}
