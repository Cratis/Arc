// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reflection;

namespace Cratis.Arc.Authorization;

/// <summary>
/// Defines a system that can evaluate authorization attributes for a type or method.
/// </summary>
public interface IAuthorizationAttributeEvaluator
{
    /// <summary>
    /// Checks if the type has an authorization attribute and retrieves role information.
    /// </summary>
    /// <param name="type">The type to check.</param>
    /// <returns>Tuple with (hasAuthorize, roles) or null if this evaluator cannot determine.</returns>
    (bool HasAuthorize, string? Roles)? GetAuthorizationInfo(Type type);

    /// <summary>
    /// Checks if the method has an authorization attribute and retrieves role information.
    /// </summary>
    /// <param name="method">The method to check.</param>
    /// <returns>Tuple with (hasAuthorize, roles) or null if this evaluator cannot determine.</returns>
    (bool HasAuthorize, string? Roles)? GetAuthorizationInfo(MethodInfo method);
}
