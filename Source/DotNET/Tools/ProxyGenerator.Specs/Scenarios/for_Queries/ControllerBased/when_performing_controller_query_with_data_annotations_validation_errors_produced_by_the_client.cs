// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.ProxyGenerator.Scenarios.Infrastructure;

namespace Cratis.Arc.ProxyGenerator.Scenarios.for_Queries.ControllerBased;

public class when_performing_controller_query_with_data_annotations_validation_errors_produced_by_the_client : given.a_scenario_web_application
{
    QueryExecutionResult<IEnumerable<ControllerQueryItem>>? _executionResult;

    void Establish()
    {
        LoadControllerQueryProxy<ControllerQueriesController>("SearchDataAnnotationsValidated");
        ControllerQueriesController.DataAnnotationsValidatedCallCount = 0;
    }

    async Task Because()
    {
        var parameters = new Dictionary<string, object>
        {
            ["email"] = "not-an-email",
            ["name"] = "ab",
            ["minAge"] = 200,
            ["website"] = "not-a-url"
        };

        _executionResult = await Bridge.PerformQueryViaProxyAsync<IEnumerable<ControllerQueryItem>>("SearchDataAnnotationsValidated", parameters);
    }

    [Fact] void should_not_be_successful() => _executionResult.Result.IsSuccess.ShouldBeFalse();
    [Fact] void should_not_be_valid() => _executionResult.Result.IsValid.ShouldBeFalse();
    [Fact] void should_have_validation_results() => _executionResult.Result.ValidationResults.ShouldNotBeEmpty();
    [Fact] void should_have_email_validation_error() => _executionResult.Result.ValidationResults.ShouldContain(_ => _.Members.Contains("email"));
    [Fact] void should_have_name_validation_error() => _executionResult.Result.ValidationResults.ShouldContain(_ => _.Members.Contains("name"));
    [Fact] void should_have_minAge_validation_error() => _executionResult.Result.ValidationResults.ShouldContain(_ => _.Members.Contains("minAge"));
    [Fact] void should_have_website_validation_error() => _executionResult.Result.ValidationResults.ShouldContain(_ => _.Members.Contains("website"));
    [Fact] void should_not_roundtrip_to_server() => ControllerQueriesController.DataAnnotationsValidatedCallCount.ShouldEqual(0);
}
