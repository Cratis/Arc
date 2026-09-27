// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Commands;

/// <summary>Optional provider-neutral checks for command-provided values and registered validators.</summary>
public interface ICommandDependencySafety
{
    /// <summary>Checks a value returned by Provide before it can become a handler dependency.</summary>
    /// <param name="value">The provided value.</param>
    void ValidateProvided(object value);

    /// <summary>Checks a registered validator before using it in this invocation.</summary>
    /// <param name="validatorType">The registered validator type.</param>
    void ValidateRegisteredValidator(Type validatorType);
}
