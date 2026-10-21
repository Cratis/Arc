// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Validation;

/// <summary>
/// Holds the discoverable validators whose final binding in the service collection, read when the built provider first
/// needs it, is a plain transient binding of the validator to itself.
/// </summary>
/// <param name="validatorTypes">The validator types bound that way.</param>
internal sealed class ConventionTransientValidators(IEnumerable<Type> validatorTypes)
{
    readonly HashSet<Type> _validatorTypes = [.. validatorTypes];

    /// <summary>
    /// Determines whether a validator type is bound as a plain transient.
    /// </summary>
    /// <param name="validatorType">The validator type.</param>
    /// <returns>True when it is; otherwise false.</returns>
    public bool Contains(Type validatorType) => _validatorTypes.Contains(validatorType);
}
