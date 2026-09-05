// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using FluentValidation;
using FluentValidation.Validators;

namespace Cratis.Arc.Validation;

/// <summary>
/// Represents an <see cref="IConceptRuleBuilder{T, TValue}"/> that decorates the real FluentValidation rule
/// builder for a <see cref="ConceptAs{TValue}"/>-typed property.
/// </summary>
/// <typeparam name="T">Type of object being validated.</typeparam>
/// <typeparam name="TValue">The primitive type wrapped by the concept.</typeparam>
/// <param name="inner">The real FluentValidation rule builder to decorate.</param>
/// <param name="owner">The <see cref="BaseValidator{T}"/> that produced this rule, to record an ignored member on.</param>
/// <param name="propertyName">The camelCased top-level property name this rule targets.</param>
/// <remarks>
/// Every <see cref="IRuleBuilder{T, TProperty}"/> member forwards to <paramref name="inner"/> and returns its
/// result unmodified. FluentValidation's own concrete rule builder implements every rule-builder interface
/// simultaneously and returns itself from each member, so this decorator never needs to re-wrap FluentValidation's
/// own return values — only <see cref="IgnoreConceptRules"/> is new behavior.
/// </remarks>
internal sealed class ConceptRuleBuilder<T, TValue>(IRuleBuilderInitial<T, TValue> inner, BaseValidator<T> owner, string propertyName)
    : IConceptRuleBuilder<T, TValue>
    where TValue : IComparable
{
    /// <inheritdoc/>
    public IRuleBuilderOptions<T, TValue> SetValidator(IPropertyValidator<T, TValue> validator) => inner.SetValidator(validator);

    /// <inheritdoc/>
    public IRuleBuilderOptions<T, TValue> SetAsyncValidator(IAsyncPropertyValidator<T, TValue> validator) => inner.SetAsyncValidator(validator);

    /// <inheritdoc/>
    public IRuleBuilderOptions<T, TValue> SetValidator(IValidator<TValue> validator, params string[] ruleSets) => inner.SetValidator(validator, ruleSets);

    /// <inheritdoc/>
    public IRuleBuilderOptions<T, TValue> SetValidator<TValidator>(Func<T, TValidator> validatorProvider, params string[] ruleSets)
        where TValidator : IValidator<TValue> => inner.SetValidator(validatorProvider, ruleSets);

    /// <inheritdoc/>
    public IRuleBuilderOptions<T, TValue> SetValidator<TValidator>(Func<T, TValue, TValidator> validatorProvider, params string[] ruleSets)
        where TValidator : IValidator<TValue> => inner.SetValidator(validatorProvider, ruleSets);

    /// <inheritdoc/>
    public IRuleBuilderInitial<T, TValue> IgnoreConceptRules()
    {
        owner.IgnoreConceptRuleFor(propertyName);
        return inner;
    }
}
