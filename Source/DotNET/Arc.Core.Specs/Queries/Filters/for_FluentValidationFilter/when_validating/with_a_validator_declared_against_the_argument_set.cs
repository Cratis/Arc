// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using FluentValidation;

namespace Cratis.Arc.Queries.Filters.for_FluentValidationFilter.when_validating;

/// <summary>
/// A validator declared against a query's flat argument shape is what the generated client proxy enforces. It has to
/// be enforced here too — a rule that only runs in the browser is not a rule, because the endpoint can be called
/// directly.
/// </summary>
public class with_a_validator_declared_against_the_argument_set : given.a_fluent_validation_filter
{
    QueryResult _result;

    void Establish()
    {
        WithParameters(new QueryParameter("email", typeof(string)), new QueryParameter("minAge", typeof(int)));
        WithArgumentsModel(new SearchArguments { Email = string.Empty, MinAge = -5 });
        WithValidatorFor(typeof(SearchArguments), new SearchArgumentsValidator());
    }

    async Task Because() => _result = await _filter.OnPerform(ContextWith(("email", string.Empty), ("minAge", -5)));

    [Fact] void should_not_be_valid() => _result.IsValid.ShouldBeFalse();
    [Fact] void should_report_both_failures() => _result.ValidationResults.Count().ShouldEqual(2);
    [Fact] void should_report_members_flat_the_way_the_client_does() => _result.ValidationResults.SelectMany(_ => _.Members).ShouldContainOnly(["Email", "MinAge"]);

    class SearchArguments
    {
        public string Email { get; set; } = string.Empty;
        public int MinAge { get; set; }
    }

    class SearchArgumentsValidator : QueryValidator<SearchArguments>
    {
        public SearchArgumentsValidator()
        {
            RuleFor(x => x.Email).NotEmpty();
            RuleFor(x => x.MinAge).GreaterThanOrEqualTo(0);
        }
    }
}
