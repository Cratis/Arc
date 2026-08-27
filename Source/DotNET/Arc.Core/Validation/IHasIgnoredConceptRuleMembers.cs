// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Validation;

/// <summary>
/// Defines a validator that can suppress the cross-cutting <see cref="ConceptValidator{T}"/> registered for one
/// of its own concept-typed properties.
/// </summary>
/// <remarks>
/// This is deliberately non-generic: <see cref="ModelGraphValidator"/> resolves validators as a bare
/// <see cref="FluentValidation.IValidator"/> against a runtime <see cref="Type"/>, and must be able to check for
/// an exclusion set without depending on <see cref="BaseValidator{T}"/>'s FluentValidation-specific generic shape.
/// </remarks>
public interface IHasIgnoredConceptRuleMembers
{
    /// <summary>
    /// Gets the camelCased top-level property names for which a registered <see cref="ConceptValidator{T}"/> (or
    /// any other <see cref="IDiscoverableValidator{T}"/> for the concept type) should not be invoked while
    /// validating this model.
    /// </summary>
    IReadOnlySet<string> IgnoredConceptRuleMembers { get; }
}
