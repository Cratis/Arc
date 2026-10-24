// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reflection;
using System.Runtime.CompilerServices;
using Cratis.Arc.Authorization;
using Cratis.Arc.Commands;
using Cratis.Arc.Commands.ModelBound;
using Cratis.Arc.Validation;
using OneOf;

namespace Cratis.Arc.Chronicle.ReadModels;

/// <summary>
/// Finds Handle parameters that cannot be supplied by Provide's declared return shape.
/// </summary>
internal static class InjectedHandleParameters
{
    /// <summary>
    /// Matches possible provided candidates in parameter order, consuming each candidate once.
    /// </summary>
    /// <param name="handle">The handler method.</param>
    /// <param name="provide">The optional provider method.</param>
    /// <returns>Parameters that still require dependency resolution for every return alternative.</returns>
    internal static IEnumerable<ParameterInfo> Of(MethodInfo handle, MethodInfo? provide)
    {
        var parameters = handle.GetParameters();
        if (provide is null) return parameters;

        var supplied = new HashSet<ParameterInfo>();
        foreach (var shape in CandidateSets(AwaitedType(provide.ReturnType), flattenTuple: true))
        {
            var candidates = shape.Where(type => !IsControlSignal(type)).ToList();
            foreach (var parameter in parameters)
            {
                if (parameter.IsDefined(typeof(GeneratedValueAttribute), inherit: false) || parameter.ParameterType == typeof(CancellationToken)) continue;
                var index = candidates.FindIndex(parameter.ParameterType.IsAssignableFrom);
                if (index < 0) continue;
                supplied.Add(parameter);
                candidates.RemoveAt(index);
            }
        }

        return parameters.Where(parameter => !supplied.Contains(parameter));
    }

    /// <summary>
    /// Unwraps the asynchronous result before flattening provided values.
    /// </summary>
    /// <param name="type">The declared return type.</param>
    /// <returns>The awaited value type, or void for a result-less task.</returns>
    static Type AwaitedType(Type type) => type.IsGenericType &&
        (type.GetGenericTypeDefinition() == typeof(Task<>) || type.GetGenericTypeDefinition() == typeof(ValueTask<>))
            ? type.GetGenericArguments()[0]
            : type == typeof(Task) || type == typeof(ValueTask) ? typeof(void) : type;

    /// <summary>
    /// Mirrors CommandProvideInvoker's union unwrapping and single-level tuple flattening.
    /// </summary>
    /// <param name="type">The value type.</param>
    /// <param name="flattenTuple">Whether a tuple is the top-level provided value.</param>
    /// <returns>Possible ordered candidate sets, keeping union alternatives separate.</returns>
    static IEnumerable<Type[]> CandidateSets(Type type, bool flattenTuple)
    {
        type = Nullable.GetUnderlyingType(type) ?? type;
        if (typeof(IOneOf).IsAssignableFrom(type))
        {
            return UnionArguments(type).SelectMany(argument => CandidateSets(argument, flattenTuple));
        }

        if (flattenTuple && typeof(ITuple).IsAssignableFrom(type))
        {
            return TupleElements(type).Aggregate(
                (IEnumerable<Type[]>)[[]],
                (sets, element) => sets.SelectMany(set => CandidateSets(element, flattenTuple: false).Select(candidates => set.Concat(candidates).ToArray())));
        }

        return type == typeof(void) ? [[]] : [[type]];
    }

    /// <summary>
    /// Finds union alternatives, including those declared by a custom OneOf base class.
    /// </summary>
    /// <param name="type">The union type.</param>
    /// <returns>The declared alternatives.</returns>
    static Type[] UnionArguments(Type type) => type.BaseType is { } parent && typeof(IOneOf).IsAssignableFrom(parent)
        ? UnionArguments(parent)
        : type.GetGenericArguments();

    /// <summary>
    /// Enumerates tuple slots, expanding the eighth rest slot but not nested tuple values.
    /// </summary>
    /// <param name="type">The tuple type.</param>
    /// <returns>The tuple's element types in runtime order.</returns>
    static IEnumerable<Type> TupleElements(Type type)
    {
        var arguments = type.GetGenericArguments();
        return arguments.Length == 8
            ? arguments.Take(7).Concat(TupleElements(arguments[7]))
            : arguments;
    }

    /// <summary>
    /// Identifies values removed before Handle's arguments are matched.
    /// </summary>
    /// <param name="type">The provided candidate type.</param>
    /// <returns>Whether the candidate is a pipeline control signal.</returns>
    static bool IsControlSignal(Type type) => typeof(CommandResult).IsAssignableFrom(type) ||
        typeof(ValidationResult).IsAssignableFrom(type) || typeof(IEnumerable<ValidationResult>).IsAssignableFrom(type) ||
        typeof(AuthorizationResult).IsAssignableFrom(type);
}
