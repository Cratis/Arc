// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.ProxyGenerator.Scenarios.Infrastructure;

namespace Cratis.Arc.ProxyGenerator.Scenarios.for_Queries.ModelBound;

[Collection(ScenarioCollectionDefinition.Name)]

public class when_performing_query_that_throws_exception : given.a_scenario_web_application
{
    QueryExecutionResult<ExceptionReadModel>? _executionResult;

    void Establish() => LoadQueryProxy<ExceptionReadModel>("GetWithException");

    async Task Because()
    {
        var parameters = new Dictionary<string, object>
        {
            ["shouldThrow"] = true
        };

        _executionResult = await Bridge.PerformQueryViaProxyAsync<ExceptionReadModel>("GetWithException", parameters);
    }

    [Fact] void should_not_be_successful() => _executionResult.Result.IsSuccess.ShouldBeFalse();
    [Fact] void should_have_exceptions() => _executionResult.Result.HasExceptions.ShouldBeTrue();
    [Fact] void should_have_exception_messages() => _executionResult.Result.ExceptionMessages.ShouldNotBeEmpty();
}
