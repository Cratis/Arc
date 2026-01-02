// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.ProxyGenerator.Scenarios.Infrastructure;

namespace Cratis.Arc.ProxyGenerator.Scenarios.for_Queries.ModelBound;

[Collection(ScenarioCollectionDefinition.Name)]

public class when_executing_data_annotations_validated_query_with_validation_errors_produced_by_the_client : given.a_scenario_web_application
{
    QueryExecutionResult<IEnumerable<DataAnnotationsValidatedReadModel>>? _executionResult;

    void Establish()
    {
        LoadQueryProxy<DataAnnotationsValidatedReadModel>("GetByEmailAndAge");
        DataAnnotationsValidatedReadModel.GetByEmailCallCount = 0;
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

        _executionResult = await Bridge.PerformQueryViaProxyAsync<IEnumerable<DataAnnotationsValidatedReadModel>>("GetByEmailAndAge", parameters);
    }

    [Fact] void should_not_be_successful() => _executionResult.Result.IsSuccess.ShouldBeFalse();
    [Fact] void should_not_be_valid() => _executionResult.Result.IsValid.ShouldBeFalse();
    [Fact] void should_have_validation_results() => _executionResult.Result.ValidationResults.ShouldNotBeEmpty();
    [Fact] void should_have_email_validation_error() => _executionResult.Result.ValidationResults.ShouldContain(_ => _.Members.Contains("email"));
    [Fact] void should_have_name_validation_error() => _executionResult.Result.ValidationResults.ShouldContain(_ => _.Members.Contains("name"));
    [Fact] void should_have_name_validation_message() => _executionResult.Result.ValidationResults.ShouldContain(_ => _.Members.Contains("name") && _.Message.Contains('3') && _.Message.Contains("50"));
    [Fact] void should_have_minAge_validation_error() => _executionResult.Result.ValidationResults.ShouldContain(_ => _.Members.Contains("minAge"));
    [Fact] void should_have_minAge_validation_message() => _executionResult.Result.ValidationResults.ShouldContain(_ => _.Members.Contains("minAge") && _.Message.Contains('0') && _.Message.Contains("150"));
    [Fact] void should_have_website_validation_error() => _executionResult.Result.ValidationResults.ShouldContain(_ => _.Members.Contains("website"));
    [Fact] void should_not_roundtrip_to_server() => DataAnnotationsValidatedReadModel.GetByEmailCallCount.ShouldEqual(0);
}
