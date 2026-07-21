// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Queries;
using Cratis.Arc.Validation;
using Cratis.Concepts;
using FluentValidation;

namespace Cratis.Arc.ProxyGenerator.ModelBound.for_QueryExtensions;

/// <summary>
/// A dependency injected into a query method, standing in for a collection or service.
/// </summary>
public interface ISomeDependency;

/// <summary>
/// An email address concept carrying its own validator.
/// </summary>
/// <param name="Value">The underlying value.</param>
public record EmailAddress(string Value) : ConceptAs<string>(Value);

/// <summary>
/// Validator for <see cref="EmailAddress"/>.
/// </summary>
public class EmailAddressValidator : ConceptValidator<EmailAddress>
{
    public EmailAddressValidator() => RuleFor(x => x.Value).EmailAddress();
}

/// <summary>
/// A read model whose query takes an injected dependency alongside its arguments.
/// </summary>
public class ReadModelWithDependency
{
    public string Term { get; set; } = string.Empty;

    public static IEnumerable<ReadModelWithDependency> Search(string term, ISomeDependency dependency) => [];
}

/// <summary>
/// Models the argument set of <see cref="ReadModelWithDependency.Search"/>, covering only the caller's arguments.
/// </summary>
public class SearchParameters
{
    public string Term { get; set; } = string.Empty;
}

/// <summary>
/// Validator for the argument set of <see cref="ReadModelWithDependency.Search"/>.
/// </summary>
public class SearchParametersValidator : QueryValidator<SearchParameters>
{
    public SearchParametersValidator() => RuleFor(x => x.Term).MinimumLength(3);
}

/// <summary>
/// A read model whose query takes a concept and also has an argument set modelled with that same concept.
/// </summary>
public class ReadModelWithConceptAndParameters
{
    public EmailAddress Email { get; set; } = new(string.Empty);

    public static IEnumerable<ReadModelWithConceptAndParameters> Lookup(EmailAddress email) => [];
}

/// <summary>
/// Models the argument set of <see cref="ReadModelWithConceptAndParameters.Lookup"/>.
/// </summary>
public class LookupParameters
{
    public EmailAddress Email { get; set; } = new(string.Empty);
}
