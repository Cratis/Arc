// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using FluentValidation;

namespace Cratis.Arc.Validation;

/// <summary>
/// Represents an <see cref="IRuleBuilderInitial{T, TProperty}"/> for a property that is a
/// <see cref="ConceptAs{TValue}"/>, adding concept-specific fluent methods on top of the ordinary
/// FluentValidation rule builder surface.
/// </summary>
/// <typeparam name="T">Type of object being validated.</typeparam>
/// <typeparam name="TValue">The primitive type wrapped by the concept.</typeparam>
public interface IConceptRuleBuilder<T, TValue> : IRuleBuilderInitial<T, TValue>
{
    /// <summary>
    /// Suppresses any cross-cutting <see cref="ConceptValidator{T}"/> (or other <see cref="IDiscoverableValidator{T}"/>)
    /// registered for this concept type, for this property on this model type only. A <see cref="ConceptValidator{T}"/>
    /// for the same concept type still runs normally for every other property, and on every other model, that
    /// carries it.
    /// </summary>
    /// <remarks>
    /// Call this directly off <c>RuleFor(...)</c>, before any other rule, e.g.
    /// <c>RuleFor(_ => _.Name).IgnoreConceptRules().NotEmpty()</c>. It cannot be called after <c>.NotEmpty()</c> or
    /// similar: those FluentValidation extension methods return FluentValidation's own <c>IRuleBuilderOptions</c>,
    /// which does not carry this member.
    /// </remarks>
    /// <returns>
    /// The underlying <see cref="IRuleBuilderInitial{T, TProperty}"/>, so the validator's own rules
    /// (<c>NotEmpty</c>, <c>Must</c>, <c>MustAsync</c>, ...) against the concept's unwrapped value can still be
    /// declared with the rest of FluentValidation's fluent surface.
    /// </returns>
    IRuleBuilderInitial<T, TValue> IgnoreConceptRules();
}
