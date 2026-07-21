// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Validation;
using Cratis.Concepts;
using FluentValidation;

namespace Cratis.Arc.Queries.Filters.for_FluentValidationFilter.when_validating;

public class with_invalid_concept_inside_a_collection_parameter : given.a_fluent_validation_filter
{
    QueryResult _result;

    void Establish()
    {
        WithParameters(new QueryParameter("emails", typeof(EmailAddress[])));
        WithValidatorFor(typeof(EmailAddress), new EmailAddressValidator());
    }

    async Task Because() => _result = await _filter.OnPerform(
        ContextWith(("emails", new EmailAddress[] { new("valid@cratis.io"), new("not-an-email"), new("also-bad") })));

    [Fact] void should_not_be_valid() => _result.IsValid.ShouldBeFalse();
    [Fact] void should_report_every_invalid_element() => _result.ValidationResults.Count().ShouldEqual(2);

    record EmailAddress(string Value) : ConceptAs<string>(Value);

    class EmailAddressValidator : ConceptValidator<EmailAddress>
    {
        public EmailAddressValidator() => RuleFor(x => x.Value).EmailAddress().WithMessage("Must be a valid email address");
    }
}
