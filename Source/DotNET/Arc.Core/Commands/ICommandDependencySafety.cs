// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Commands;

/// <summary>
/// Optional provider-neutral checks for values returned by a protected or unprotected command's Provide method.
/// </summary>
public interface ICommandDependencySafety
{
    /// <summary>
    /// Checks a value returned by Provide before it can become a handler dependency.
    /// </summary>
    /// <param name="value">The provided value.</param>
    void ValidateProvided(object value);
}
