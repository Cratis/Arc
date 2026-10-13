// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Screenplay.Analysis.Events;
using Microsoft.CodeAnalysis;

namespace Cratis.Arc.Screenplay.Analysis.Projections;

/// <summary>
/// Recognizes only the static reductions that make a concrete model its own reducer.
/// </summary>
internal static class SelfReducerMethods
{
    /// <summary>
    /// Gets eligible reductions from the model's declared hierarchy.
    /// </summary>
    /// <param name="model">The candidate model.</param>
    /// <returns>The eligible static reductions, or an empty sequence for classic reducers.</returns>
    internal static IEnumerable<IMethodSymbol> Of(INamedTypeSymbol model) => CanBeSelf(model)
        ? MethodsOf(model).Where(method => IsReduction(model, method))
        : [];

    static bool CanBeSelf(INamedTypeSymbol model) => model is { TypeKind: TypeKind.Class, IsAbstract: false, IsGenericType: false } &&
        !IsOpen(model) && !model.AllInterfaces.Any(type => type.Is(WellKnownTypeNames.ReducerFor) || type.Is("Cratis.Chronicle.Reducers.IReducer"));

    static bool IsOpen(INamedTypeSymbol model) => model.TypeParameters.Length > 0 || (model.ContainingType is { } parent && IsOpen(parent));

    static IEnumerable<IMethodSymbol> MethodsOf(INamedTypeSymbol model)
    {
        for (var current = model; current is not null; current = current.BaseType)
        {
            foreach (var method in current.GetMembers().OfType<IMethodSymbol>())
            {
                yield return method;
            }
        }
    }

    static bool IsReduction(INamedTypeSymbol model, IMethodSymbol method) =>
        method.Name == "Reduce" && method is { MethodKind: MethodKind.Ordinary, IsStatic: true, DeclaredAccessibility: Accessibility.Public, TypeParameters.Length: 0, ReturnsByRef: false, ReturnsByRefReadonly: false } &&
        method.Parameters.Length is 2 or 3 &&
        method.Parameters.All(parameter => parameter.RefKind == RefKind.None && !parameter.IsOptional && !parameter.IsParams) &&
        EventReader.IsEvent(method.Parameters[0].Type) &&
        SymbolEqualityComparer.Default.Equals(method.Parameters[1].Type, model) &&
        method.Parameters[1].NullableAnnotation != NullableAnnotation.NotAnnotated &&
        (method.Parameters.Length == 2 || method.Parameters[2].Type.Is(WellKnownTypeNames.EventContext)) &&
        SymbolEqualityComparer.Default.Equals(UnwrapTask(method.ReturnType), model);

    static ITypeSymbol UnwrapTask(ITypeSymbol type) =>
        type is INamedTypeSymbol { TypeArguments.Length: 1 } task && task.Name == "Task" && task.ContainingNamespace.ToDisplayString() == "System.Threading.Tasks"
            ? task.TypeArguments[0]
            : type;
}
