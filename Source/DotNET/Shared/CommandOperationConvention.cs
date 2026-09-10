// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.CodeAnalysis;

namespace Cratis.Arc;

/// <summary>
/// Shared Roslyn authoring contract used by the analyzer and typed invoker generator.
/// </summary>
internal static class CommandOperationConvention
{
    /// <summary>
    /// Fully qualified operation marker.
    /// </summary>
    public const string Marker = "Cratis.Arc.Commands.ICommandOperation";

    /// <summary>
    /// Fully qualified explicit batch.
    /// </summary>
    public const string Batch = "Cratis.Arc.Commands.CommandOperations";

    /// <summary>
    /// Fully qualified recovery context.
    /// </summary>
    public const string Failure = "Cratis.Arc.Commands.CommandOperationFailure";

    /// <summary>
    /// Determines whether a type opts into server-side operation execution.
    /// </summary>
    /// <param name="type">Type to inspect.</param>
    /// <returns>Whether the marker is implemented.</returns>
    public static bool IsOperation(ITypeSymbol type) => TypeName(type) == Marker || type.AllInterfaces.Any(contract => TypeName(contract) == Marker);

    /// <summary>
    /// Finds conventional methods, including inherited declarations.
    /// </summary>
    /// <param name="type">Declaration type.</param>
    /// <param name="name">Conventional method name.</param>
    /// <returns>Matching methods.</returns>
    public static IMethodSymbol[] Methods(INamedTypeSymbol type, string name)
    {
        var methods = type.GetMembers(name).OfType<IMethodSymbol>().ToArray();
        var inherited = type.BaseType is null ? [] : Methods(type.BaseType, name)
            .Where(method => method.DeclaredAccessibility != Accessibility.Private && !methods.Any(declared =>
                declared.Arity == method.Arity && declared.Parameters.Length == method.Parameters.Length &&
                declared.Parameters.Zip(method.Parameters, (left, right) => left.RefKind == right.RefKind && SymbolEqualityComparer.Default.Equals(left.Type, right.Type)).All(equal => equal)))
            .ToArray();

        return [.. methods, .. inherited];
    }

    /// <summary>
    /// Validates a method before generating any user invocation.
    /// </summary>
    /// <param name="method">Method to inspect.</param>
    /// <param name="compensate">Whether failure context is allowed.</param>
    /// <returns>Whether the initial no-receipt contract is satisfied.</returns>
    public static bool IsValid(IMethodSymbol method, bool compensate) =>
        method.DeclaredAccessibility == Accessibility.Public && !method.IsStatic && !method.IsGenericMethod && !method.IsAbstract &&
        (method.ReturnsVoid || method.ReturnType.ToDisplayString() == "System.Threading.Tasks.Task" || method.ReturnType.ToDisplayString() == "System.Threading.Tasks.ValueTask") &&
        !(method.IsAsync && method.ReturnsVoid) &&
        method.Parameters.All(parameter => parameter.RefKind == RefKind.None && !parameter.IsOptional && !parameter.IsParams && parameter.Type.TypeKind != TypeKind.Pointer &&
            !IsServiceLocator(parameter.Type) && (compensate || TypeName(parameter.Type) != Failure)) &&
        method.Parameters.Count(parameter => TypeName(parameter.Type) == "System.Threading.CancellationToken") <= 1 &&
        method.Parameters.Count(parameter => TypeName(parameter.Type) == Failure) <= 1;

    /// <summary>
    /// Checks whether generated assembly-level code can reference a concrete declaration.
    /// </summary>
    /// <param name="type">Declaration type.</param>
    /// <returns>Whether direct typed calls can be emitted.</returns>
    public static bool IsAccessible(INamedTypeSymbol type) =>
        type.Arity == 0 && type.DeclaredAccessibility is Accessibility.Public or Accessibility.Internal &&
        (type.ContainingType is null || IsAccessible(type.ContainingType));

    static bool IsServiceLocator(ITypeSymbol type) => IsServiceLocatorName(TypeName(type)) || type.AllInterfaces.Any(contract => IsServiceLocatorName(TypeName(contract)));

    static bool IsServiceLocatorName(string name) => name == "System.IServiceProvider" || name == "Microsoft.Extensions.DependencyInjection.IServiceScopeFactory" || name == "Microsoft.Extensions.DependencyInjection.IServiceScope";

    static string TypeName(ITypeSymbol type) => type.WithNullableAnnotation(NullableAnnotation.NotAnnotated).ToDisplayString();
}
