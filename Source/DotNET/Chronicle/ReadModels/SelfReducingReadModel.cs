// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reflection;
using Cratis.Chronicle.Events;
using Cratis.Chronicle.Reducers;

namespace Cratis.Arc.Chronicle.ReadModels;

/// <summary>
/// Recognizes self-reducing models using APIs available before Chronicle 19.58.0.
/// </summary>
internal static class SelfReducingReadModel
{
    /// <summary>
    /// Tests the self shape, not merely membership in the artifacts provider's reducer set.
    /// </summary>
    /// <param name="model">The candidate model.</param>
    /// <returns>Whether the type declares an eligible static reduction.</returns>
    internal static bool IsSelfReducing(Type model) =>
        model.IsClass && !model.IsAbstract && !model.IsGenericType && !model.ContainsGenericParameters &&
        !model.IsAssignableTo(typeof(IReducer)) &&
        !model.GetInterfaces().Any(type => type.IsGenericType && type.GetGenericTypeDefinition() == typeof(IReducerFor<>)) &&
        MethodsOf(model).Any(method => IsReduction(model, method));

    static IEnumerable<MethodInfo> MethodsOf(Type model)
    {
        for (var current = model; current is not null; current = current.BaseType)
        {
            foreach (var method in current.GetMethods(BindingFlags.Public | BindingFlags.Static | BindingFlags.DeclaredOnly))
            {
                yield return method;
            }
        }
    }

    static bool IsReduction(Type model, MethodInfo method)
    {
        var parameters = method.GetParameters();
        return method.Name == "Reduce" && !method.IsSpecialName && !method.IsGenericMethod && !method.ContainsGenericParameters &&
            parameters.Length is 2 or 3 &&
            parameters.All(parameter => !parameter.ParameterType.IsByRef && !parameter.IsOptional && !parameter.IsDefined(typeof(ParamArrayAttribute))) &&
            parameters[0].ParameterType.IsDefined(typeof(EventTypeAttribute), inherit: true) &&
            parameters[1].ParameterType == model &&
            new NullabilityInfoContext().Create(parameters[1]).ReadState != NullabilityState.NotNull &&
            (parameters.Length == 2 || parameters[2].ParameterType == typeof(EventContext)) &&
            (method.ReturnType == model || method.ReturnType == typeof(Task<>).MakeGenericType(model));
    }
}
