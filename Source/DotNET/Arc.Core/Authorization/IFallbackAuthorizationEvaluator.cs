// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reflection;

namespace Cratis.Arc.Authorization;

/// <summary>
/// Supplies baseline authorization requirements only when neither the method nor its declaring type has an explicit authorization declaration.
/// </summary>
/// <remarks>
/// All requirements from all fallback evaluators must pass. Explicit declarations, including anonymous access, replace the baseline.
/// Implementations are discovered by convention and evaluated for each authorization check.
/// </remarks>
public interface IFallbackAuthorizationEvaluator
{
    /// <summary>
    /// Gets the baseline requirements for a type without an explicit declaration.
    /// </summary>
    /// <param name="type">The type to check.</param>
    /// <returns>Requirements that must all be satisfied, or an empty sequence when this evaluator does not apply.</returns>
    IEnumerable<AuthorizationRequirement> GetAuthorizationRequirements(Type type);

    /// <summary>
    /// Gets the baseline requirements for a method without an explicit declaration.
    /// </summary>
    /// <param name="method">The method to check.</param>
    /// <returns>Requirements that must all be satisfied, or an empty sequence when this evaluator does not apply.</returns>
    IEnumerable<AuthorizationRequirement> GetAuthorizationRequirements(MethodInfo method);
}
