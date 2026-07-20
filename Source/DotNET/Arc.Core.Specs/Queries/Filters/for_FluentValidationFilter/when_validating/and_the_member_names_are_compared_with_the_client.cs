// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Validation;
using Cratis.Concepts;
using FluentValidation;

namespace Cratis.Arc.Queries.Filters.for_FluentValidationFilter.when_validating;

/// <summary>
/// A form matches a validation failure to the field that caused it by member name, so the two sides have to name
/// members the same way or a server rejection highlights nothing. The generated client models everything in
/// TypeScript casing and erases a concept to its primitive, reporting <c>email</c>; this pins the server to the same
/// answer rather than the <c>Email</c> and <c>email.Value</c> it would otherwise produce.
/// </summary>
public class and_the_member_names_are_compared_with_the_client : given.a_fluent_validation_filter
{
    QueryResult _conceptResult;
    QueryResult _argumentSetResult;

    void Establish()
    {
        WithParameters(new QueryParameter("email", typeof(EmailAddress)));
        WithValidatorFor(typeof(EmailAddress), new EmailAddressValidator());
        WithValidatorFor(typeof(SearchArguments), new SearchArgumentsValidator());
    }

    async Task Because()
    {
        _conceptResult = await _filter.OnPerform(ContextWith(("email", new EmailAddress("not-an-email"))));

        WithArgumentsModel(new SearchArguments { Email = string.Empty });
        _argumentSetResult = await _filter.OnPerform(ContextWith(("email", string.Empty)));
    }

    [Fact] void should_camel_case_a_member_from_an_argument_set() => _argumentSetResult.ValidationResults.Single().Members.ShouldContainOnly("email");
    [Fact] void should_drop_the_concepts_inner_member() => _conceptResult.ValidationResults.Single().Members.ShouldContainOnly("email");
    [Fact] void should_report_the_same_member_either_way() =>
        _conceptResult.ValidationResults.Single().Members.ShouldEqual(_argumentSetResult.ValidationResults.Single().Members);

    record EmailAddress(string Value) : ConceptAs<string>(Value);

    class EmailAddressValidator : ConceptValidator<EmailAddress>
    {
        public EmailAddressValidator() => RuleFor(x => x.Value).EmailAddress();
    }

    class SearchArguments
    {
        public string Email { get; set; } = string.Empty;
    }

    class SearchArgumentsValidator : QueryValidator<SearchArguments>
    {
        public SearchArgumentsValidator() => RuleFor(x => x.Email).NotEmpty();
    }
}
