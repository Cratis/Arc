// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Validation;
using Cratis.Concepts;
using FluentValidation;

namespace Cratis.Arc.Queries.Filters.for_FluentValidationFilter.when_validating;

public class with_a_valid_concept_parameter : given.a_fluent_validation_filter
{
    QueryResult _result;

    void Establish()
    {
        WithParameters(new QueryParameter("email", typeof(EmailAddress)));
        WithValidatorFor(typeof(EmailAddress), new EmailAddressValidator());
    }

    async Task Because() => _result = await _filter.OnPerform(ContextWith(("email", new EmailAddress("author@cratis.io"))));

    [Fact] void should_be_successful() => _result.IsSuccess.ShouldBeTrue();
    [Fact] void should_be_valid() => _result.IsValid.ShouldBeTrue();
    [Fact] void should_have_no_validation_errors() => _result.ValidationResults.ShouldBeEmpty();

    record EmailAddress(string Value) : ConceptAs<string>(Value);

    class EmailAddressValidator : ConceptValidator<EmailAddress>
    {
        public EmailAddressValidator() => RuleFor(x => x.Value).EmailAddress().WithMessage("Must be a valid email address");
    }
}
