// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.ProxyGenerator.Scenarios.Infrastructure;

namespace Cratis.Arc.ProxyGenerator.Scenarios.for_Queries.ModelBound;

public class when_performing_query_with_validation_errors_produced_by_the_client : given.a_scenario_web_application
{
    QueryExecutionResult<IEnumerable<FluentValidatedReadModel>>? _executionResult;

    void Establish()
    {
        LoadQueryProxy<FluentValidatedReadModel>("GetByEmailAndAge");
        FluentValidatedReadModel.GetByEmailCallCount = 0;
    }

    async Task Because()
    {
        var parameters = new Dictionary<string, object>
        {
            ["email"] = "invalid-email",
            ["minAge"] = -10
        };

        _executionResult = await Bridge.PerformQueryViaProxyAsync<IEnumerable<FluentValidatedReadModel>>("GetByEmailAndAge", parameters);
    }

    [Fact] void should_not_be_successful() => _executionResult.Result.IsSuccess.ShouldBeFalse();
    [Fact] void should_not_be_valid() => _executionResult.Result.IsValid.ShouldBeFalse();
    [Fact] void should_have_validation_results() => _executionResult.Result.ValidationResults.ShouldNotBeEmpty();
    [Fact] void should_have_email_validation_error() => _executionResult.Result.ValidationResults.ShouldContain(v => v.Members.Contains("email"));
    [Fact] void should_have_email_validation_message() => _executionResult.Result.ValidationResults.ShouldContain(v => v.Members.Contains("email") && v.Message == FluentValidatedReadModelGetByEmailAndAgeValidator.EmailRequiredMessage);
    [Fact] void should_have_minAge_validation_error() => _executionResult.Result.ValidationResults.ShouldContain(v => v.Members.Contains("minAge"));
    [Fact] void should_have_minAge_validation_message() => _executionResult.Result.ValidationResults.ShouldContain(v => v.Members.Contains("minAge") && v.Message == "Value must be greater than or equal to 0");
    [Fact] void should_not_roundtrip_to_server() => FluentValidatedReadModel.GetByEmailCallCount.ShouldEqual(0);
}
