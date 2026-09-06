// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.ProxyGenerator.Scenarios.Infrastructure;

namespace Cratis.Arc.ProxyGenerator.Scenarios.for_Queries.ModelBound;

/// <summary>
/// Verifies that an <c>IEnumerable&lt;ReadModelStatus&gt;</c> query parameter - a collection of enums - survives
/// proxy generation, reaches the server as a query argument, and binds to the actual values the caller sent.
/// </summary>
/// <remarks>
/// A bare enum parameter already qualified as a query argument before this fix (it is a value type, excluded from
/// dependency resolution before the container is consulted); a collection of enums did not, since
/// <c>IEnumerable&lt;T&gt;</c> is a reference type and the built-in <c>IServiceProviderIsService</c> answers true
/// for it unconditionally. This spec proves the collection-of-enum shape now binds end to end, the same way
/// collections of primitives and concepts do.
/// <para>
/// Asserts against <c>RawJson</c> - the server's response body captured before the JS test harness's
/// instance-shaped deserialization patch runs - rather than <c>Result.Data</c>, because that patch is not written
/// for collection results and reduces an array to an empty object.
/// </para>
/// </remarks>
[Collection(ScenarioCollectionDefinition.Name)]
public class when_performing_query_with_enumerable_enum_parameter_and_checking_data : given.a_scenario_web_application
{
    QueryExecutionResult<IEnumerable<EnumParameterReadModel>>? _executionResult;

    void Establish() => LoadQueryProxy<EnumParameterReadModel>("SearchByStatuses");

    async Task Because()
    {
        var parameters = new Dictionary<string, object>
        {
            ["statuses"] = new[] { (int)ReadModelStatus.Active, (int)ReadModelStatus.Archived }
        };

        _executionResult = await Bridge.PerformQueryViaProxyAsync<IEnumerable<EnumParameterReadModel>>("SearchByStatuses", parameters);
    }

    [Fact] void should_return_successful_result() => _executionResult.Result.IsSuccess.ShouldBeTrue();
    [Fact] void should_include_first_status_in_url() => _executionResult.RequestUrl.ShouldContain($"statuses={(int)ReadModelStatus.Active}");
    [Fact] void should_include_second_status_in_url() => _executionResult.RequestUrl.ShouldContain($"statuses={(int)ReadModelStatus.Archived}");
    [Fact] void should_return_item_for_first_status() => _executionResult.RawJson.ShouldContain($"Item {nameof(ReadModelStatus.Active)}");
    [Fact] void should_return_item_for_second_status() => _executionResult.RawJson.ShouldContain($"Item {nameof(ReadModelStatus.Archived)}");
}
