// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reflection;
using Cratis.Arc.Commands;
using Cratis.Chronicle.ReadModels;

namespace Cratis.Arc.Chronicle.Testing.Commands;

/// <summary>
/// Finds the read models a <c>[ProtectedDecision]</c> command reads through <see cref="DecisionRead{T}"/> parameters.
/// </summary>
/// <remarks>
/// Only parameters are statically visible. Explicit <see cref="IDecisionReads.Get{T}"/> calls inside a method body are not.
/// </remarks>
internal static class ProtectedDecisionReadParameters
{
    const BindingFlags Methods = BindingFlags.DeclaredOnly | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static;

    /// <summary>
    /// Checks whether a command is marked as a protected decision.
    /// </summary>
    /// <param name="commandType">The command type.</param>
    /// <returns>True if the command is protected.</returns>
    public static bool IsProtected(Type commandType) =>
        commandType.GetCustomAttributes(true).Any(_ => _ is IProtectedDecisionAttribute);

    /// <summary>
    /// Gets the read model types of the protected decision read parameters of a command.
    /// </summary>
    /// <param name="commandType">The command type.</param>
    /// <returns>The distinct read model types, or none when the command is not protected.</returns>
    public static IEnumerable<Type> ReadModelTypesOf(Type commandType) =>
        IsProtected(commandType)
            ? commandType.GetMethods(Methods)
                .SelectMany(_ => _.GetParameters())
                .Select(_ => _.ParameterType)
                .Where(_ => _.IsGenericType && _.GetGenericTypeDefinition() == typeof(DecisionRead<>))
                .Select(_ => _.GetGenericArguments()[0])
                .Distinct()
                .ToArray()
            : [];
}
