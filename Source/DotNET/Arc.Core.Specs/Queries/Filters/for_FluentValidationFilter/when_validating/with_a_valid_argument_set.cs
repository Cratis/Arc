// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using FluentValidation;

namespace Cratis.Arc.Queries.Filters.for_FluentValidationFilter.when_validating;

/// <summary>
/// The counterpart to rejecting an invalid argument set — enforcement must let a legitimate request through, or a
/// validator that rejected everything would look correct.
/// </summary>
public class with_a_valid_argument_set : given.a_fluent_validation_filter
{
    QueryResult _result;

    void Establish()
    {
        WithParameters(new QueryParameter("email", typeof(string)), new QueryParameter("minAge", typeof(int)));
        WithArgumentsModel(new SearchArguments { Email = "author@cratis.io", MinAge = 21 });
        WithValidatorFor(typeof(SearchArguments), new SearchArgumentsValidator());
    }

    async Task Because() => _result = await _filter.OnPerform(ContextWith(("email", "author@cratis.io"), ("minAge", 21)));

    [Fact] void should_be_successful() => _result.IsSuccess.ShouldBeTrue();
    [Fact] void should_be_valid() => _result.IsValid.ShouldBeTrue();
    [Fact] void should_have_no_validation_errors() => _result.ValidationResults.ShouldBeEmpty();

    class SearchArguments
    {
        public string Email { get; set; } = string.Empty;
        public int MinAge { get; set; }
    }

    class SearchArgumentsValidator : QueryValidator<SearchArguments>
    {
        public SearchArgumentsValidator()
        {
            RuleFor(x => x.Email).NotEmpty().EmailAddress();
            RuleFor(x => x.MinAge).GreaterThanOrEqualTo(0);
        }
    }
}
