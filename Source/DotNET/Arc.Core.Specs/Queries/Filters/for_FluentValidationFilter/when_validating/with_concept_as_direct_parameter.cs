// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Validation;
using Cratis.Concepts;
using FluentValidation;

namespace Cratis.Arc.Queries.Filters.for_FluentValidationFilter.when_validating;

public class with_concept_as_direct_parameter : given.a_fluent_validation_filter
{
    QueryResult _result;

    void Establish()
    {
        WithParameters(new QueryParameter("email", typeof(EmailAddress)));
        WithValidatorFor(typeof(EmailAddress), new EmailAddressValidator());
    }

    async Task Because() => _result = await _filter.OnPerform(ContextWith(("email", new EmailAddress("not-an-email"))));

    [Fact] void should_not_be_valid() => _result.IsValid.ShouldBeFalse();
    [Fact] void should_have_a_single_validation_error() => _result.ValidationResults.Count().ShouldEqual(1);
    [Fact] void should_attribute_the_failure_to_the_parameter() => _result.ValidationResults.Single().Members.ShouldContainOnly("email");

    record EmailAddress(string Value) : ConceptAs<string>(Value);

    class EmailAddressValidator : ConceptValidator<EmailAddress>
    {
        public EmailAddressValidator() => RuleFor(x => x.Value).EmailAddress().WithMessage("Must be a valid email address");
    }
}
