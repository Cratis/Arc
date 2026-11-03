// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Validation;
using FluentValidation;

namespace Cratis.Arc.Queries.Filters.for_FluentValidationFilter.when_validating;

public class with_a_query_only_rule : given.a_fluent_validation_filter
{
    const string Message = "A query needs a search term.";
    QueryResult _result;

    void Establish()
    {
        WithParameters(new QueryParameter("term", typeof(string)));
        WithArgumentsModel(new SearchArguments { Term = string.Empty });
        WithValidatorFor(typeof(SearchArguments), new SearchArgumentsValidator());
    }

    async Task Because() => _result = await _filter.OnPerform(ContextWith(("term", string.Empty)));

    [Fact] void should_not_be_valid() => _result.IsValid.ShouldBeFalse();
    [Fact] void should_report_the_rule_message() => _result.ValidationResults.Single().Message.ShouldEqual(Message);
    [Fact] void should_not_report_the_validator_failing() => _result.ValidationResults.Single().Reason.ShouldEqual(ValidationResultReason.Rule);

    class SearchArguments
    {
        public string Term { get; set; } = string.Empty;
    }

    class SearchArgumentsValidator : BaseValidator<SearchArguments>
    {
        public SearchArgumentsValidator() => WhenQuery(() => RuleFor(x => x.Term).NotEmpty().WithMessage(Message));
    }
}
