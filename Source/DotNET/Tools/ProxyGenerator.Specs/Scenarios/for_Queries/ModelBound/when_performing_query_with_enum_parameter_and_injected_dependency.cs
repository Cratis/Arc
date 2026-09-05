// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.ProxyGenerator.Scenarios.Infrastructure;

namespace Cratis.Arc.ProxyGenerator.Scenarios.for_Queries.ModelBound;

/// <summary>
/// Verifies that an enum argument and an injected dependency on the same query are classified apart from each other.
/// </summary>
/// <remarks>
/// One predicate decides both, so the two failures are mirror images and a fix for either can cause the other: an
/// enum treated as a dependency is looked up in the container and fails the request, while a dependency treated as
/// an argument is expected from the caller and never resolved. This is the shape the defect was reported against.
/// </remarks>
[Collection(ScenarioCollectionDefinition.Name)]
public class when_performing_query_with_enum_parameter_and_injected_dependency : given.a_scenario_web_application
{
    QueryExecutionResult<IEnumerable<EnumParameterReadModel>>? _executionResult;

    void Establish() => LoadQueryProxy<EnumParameterReadModel>("SearchByStatusWithDependency");

    async Task Because()
    {
        var parameters = new Dictionary<string, object>
        {
            ["status"] = (int)ReadModelStatus.Archived
        };

        _executionResult = await Bridge.PerformQueryViaProxyAsync<IEnumerable<EnumParameterReadModel>>("SearchByStatusWithDependency", parameters);
    }

    [Fact] void should_return_successful_result() => _executionResult.Result.IsSuccess.ShouldBeTrue();
    [Fact] void should_include_the_enum_value_in_url() => _executionResult.RequestUrl.ShouldContain($"status={(int)ReadModelStatus.Archived}");
    [Fact] void should_not_expect_the_dependency_from_the_caller() => _executionResult.RequestUrl.ShouldNotContain("dependency=");
    [Fact] void should_resolve_the_dependency_and_pass_the_enum_to_it() => _executionResult.RawJson.ShouldContain($"Resolved {nameof(ReadModelStatus.Archived)}");
}
