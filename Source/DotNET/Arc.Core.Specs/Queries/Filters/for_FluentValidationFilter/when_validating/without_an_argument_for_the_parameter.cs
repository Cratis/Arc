// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Validation;
using Cratis.Concepts;
using FluentValidation;

namespace Cratis.Arc.Queries.Filters.for_FluentValidationFilter.when_validating;

/// <summary>
/// A missing argument is the performer's concern, not this filter's — a validator cannot judge a value that was
/// never supplied, and reporting it here would duplicate the required-argument failure.
/// </summary>
public class without_an_argument_for_the_parameter : given.a_fluent_validation_filter
{
    QueryResult _result;

    void Establish()
    {
        WithParameters(new QueryParameter("email", typeof(EmailAddress), true));
        WithValidatorFor(typeof(EmailAddress), new EmailAddressValidator());
    }

    async Task Because() => _result = await _filter.OnPerform(ContextWith());

    [Fact] void should_be_valid() => _result.IsValid.ShouldBeTrue();
    [Fact] void should_have_no_validation_errors() => _result.ValidationResults.ShouldBeEmpty();

    record EmailAddress(string Value) : ConceptAs<string>(Value);

    class EmailAddressValidator : ConceptValidator<EmailAddress>
    {
        public EmailAddressValidator() => RuleFor(x => x.Value).EmailAddress().WithMessage("Must be a valid email address");
    }
}
