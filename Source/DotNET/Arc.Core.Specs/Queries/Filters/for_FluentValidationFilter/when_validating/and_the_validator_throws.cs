// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Validation;
using Cratis.Concepts;
using FluentValidation;

namespace Cratis.Arc.Queries.Filters.for_FluentValidationFilter.when_validating;

/// <summary>
/// A validator that dereferences a null member throws while validating partial or hostile input. That is invalid
/// input, not a server fault, so it must surface as a validation failure rather than escaping as an error result.
/// </summary>
public class and_the_validator_throws : given.a_fluent_validation_filter
{
    QueryResult _result;

    void Establish()
    {
        WithParameters(new QueryParameter("filter", typeof(AuthorFilter)));
        WithValidatorFor(typeof(AuthorFilter), new AuthorFilterValidator());
    }

    async Task Because() => _result = await _filter.OnPerform(ContextWith(("filter", new AuthorFilter(null!))));

    [Fact] void should_not_be_valid() => _result.IsValid.ShouldBeFalse();
    [Fact] void should_not_surface_as_an_exception() => _result.HasExceptions.ShouldBeFalse();
    [Fact] void should_surface_the_generic_validation_message() => _result.ValidationResults.Single().Message.ShouldEqual(ModelGraphValidator.CouldNotValidateMessage);

    record EmailAddress(string Value) : ConceptAs<string>(Value);
    record AuthorFilter(EmailAddress Email);

    class AuthorFilterValidator : QueryValidator<AuthorFilter>
    {
        public AuthorFilterValidator() => RuleFor(x => x.Email.Value).NotEmpty();
    }
}
