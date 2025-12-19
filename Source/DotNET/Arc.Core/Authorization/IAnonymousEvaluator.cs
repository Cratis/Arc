// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reflection;

namespace Cratis.Arc.Authorization;

/// <summary>
/// Defines a system that can evaluate if anonymous access is allowed for a type or method.
/// </summary>
public interface IAnonymousEvaluator
{
    /// <summary>
    /// Checks if anonymous access is allowed for the specified type.
    /// </summary>
    /// <param name="type">The type to check.</param>
    /// <returns>True if anonymous access is allowed, false otherwise, or null if this evaluator cannot determine.</returns>
    bool? IsAnonymousAllowed(Type type);

    /// <summary>
    /// Checks if anonymous access is allowed for the specified method.
    /// </summary>
    /// <param name="method">The method to check.</param>
    /// <returns>True if anonymous access is allowed, false otherwise, or null if this evaluator cannot determine.</returns>
    bool? IsAnonymousAllowed(MethodInfo method);
}
