// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Validation;
using Cratis.Concepts;
using FluentValidation;

namespace Cratis.Arc.Queries.Filters.for_FluentValidationFilter.when_validating;

/// <summary>
/// The parameter type itself has no validator — only the concept nested inside it does. The traversal must still
/// descend, otherwise a validator that fires on a command silently does nothing on a query carrying the same value.
/// </summary>
public class with_concept_nested_inside_a_parameter_object : given.a_fluent_validation_filter
{
    QueryResult _result;

    void Establish()
    {
        WithParameters(new QueryParameter("filter", typeof(AuthorFilter)));
        WithValidatorFor(typeof(EmailAddress), new EmailAddressValidator());
    }

    async Task Because() => _result = await _filter.OnPerform(ContextWith(("filter", new AuthorFilter(new EmailAddress("not-an-email")))));

    [Fact] void should_not_be_valid() => _result.IsValid.ShouldBeFalse();
    [Fact] void should_have_a_single_validation_error() => _result.ValidationResults.Count().ShouldEqual(1);
    [Fact] void should_attribute_the_failure_to_the_owning_field() => _result.ValidationResults.Single().Members.ShouldContainOnly("filter.email.Value");

    record EmailAddress(string Value) : ConceptAs<string>(Value);
    record AuthorFilter(EmailAddress Email);

    class EmailAddressValidator : ConceptValidator<EmailAddress>
    {
        public EmailAddressValidator() => RuleFor(x => x.Value).EmailAddress().WithMessage("Must be a valid email address");
    }
}
