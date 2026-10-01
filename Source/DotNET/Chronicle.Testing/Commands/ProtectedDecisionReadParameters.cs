// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reflection;
using Cratis.Arc.Commands;
using Cratis.Arc.Commands.ModelBound;
using Cratis.Chronicle.ReadModels;

namespace Cratis.Arc.Chronicle.Testing.Commands;

/// <summary>
/// Finds the read models a <c>[ProtectedDecision]</c> command reads through <see cref="DecisionRead{T}"/> parameters.
/// </summary>
/// <remarks>
/// <para>
/// Discovery mirrors how the pipeline resolves the dependencies of a command: the parameters of its <c>Handle</c> and
/// <c>Provide</c> instance methods, public or not, declared on the command or inherited from a base type. A
/// <c>DecisionRead&lt;T&gt;</c> parameter is a decision read whether or not it is annotated as nullable, and so is
/// an <c>IEnumerable&lt;DecisionRead&lt;T&gt;&gt;</c>, which the container resolves from the same registration.
/// Parameters of other methods are never injected, so they are not decision reads.
/// </para>
/// <para>
/// Only parameters are statically visible. Explicit <see cref="IDecisionReads.Get{T}"/> calls inside a method body are not.
/// </para>
/// </remarks>
internal static class ProtectedDecisionReadParameters
{
    const BindingFlags Methods = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance;
    const string HandleMethodName = "Handle";

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
                .Where(_ => _.Name == HandleMethodName || _.Name == CommandProvideMethodExtensions.ProvideMethodName)
                .SelectMany(_ => _.GetParameters())
                .Select(_ => ReadModelTypeOf(_.ParameterType))
                .OfType<Type>()
                .Where(_ => !_.ContainsGenericParameters)
                .Distinct()
                .ToArray()
            : [];

    static Type? ReadModelTypeOf(Type parameterType)
    {
        if (IsDecisionRead(parameterType))
        {
            return parameterType.GetGenericArguments()[0];
        }

        return parameterType.IsGenericType &&
            parameterType.GetGenericTypeDefinition() == typeof(IEnumerable<>) &&
            IsDecisionRead(parameterType.GetGenericArguments()[0])
                ? parameterType.GetGenericArguments()[0].GetGenericArguments()[0]
                : null;
    }

    static bool IsDecisionRead(Type type) => type.IsGenericType && type.GetGenericTypeDefinition() == typeof(DecisionRead<>);
}
