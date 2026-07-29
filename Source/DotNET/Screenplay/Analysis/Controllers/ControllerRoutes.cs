// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.CodeAnalysis;

namespace Cratis.Arc.Screenplay.Analysis.Controllers;

/// <summary>
/// Recognizes the methods of a controller and what kind of artifact each one is.
/// </summary>
/// <remarks>
/// Controllers are matched by the name of the base type and of the verb attributes rather than by referencing
/// ASP.NET Core, which keeps the generator free of a web framework it only ever needs to recognize.
/// </remarks>
public static class ControllerRoutes
{
    /// <summary>
    /// The namespace the verb attributes live in.
    /// </summary>
    public const string MvcNamespace = "Microsoft.AspNetCore.Mvc";

    static readonly string[] _mutating =
    [
        "HttpPostAttribute",
        "HttpPutAttribute",
        "HttpDeleteAttribute",
        "HttpPatchAttribute"
    ];

    /// <summary>
    /// Determines whether a type is a controller.
    /// </summary>
    /// <param name="type">The type to check.</param>
    /// <returns>True when the type derives from the ASP.NET Core controller base type.</returns>
    public static bool IsController(ITypeSymbol type) =>
        type is { IsAbstract: false, TypeKind: TypeKind.Class } && type.FindBase(WellKnownTypeNames.ControllerBase) is not null;

    /// <summary>
    /// Determines whether a method changes state.
    /// </summary>
    /// <param name="method">The method to check.</param>
    /// <returns>True when the method carries a mutating verb.</returns>
    public static bool IsCommand(IMethodSymbol method) => _mutating.Any(verb => Carries(method, verb));

    /// <summary>
    /// Determines whether a method reads state.
    /// </summary>
    /// <param name="method">The method to check.</param>
    /// <returns>True when the method carries the read verb.</returns>
    public static bool IsQuery(IMethodSymbol method) => Carries(method, "HttpGetAttribute");

    /// <summary>
    /// Gets the route a controller or one of its methods is served at.
    /// </summary>
    /// <param name="symbol">The controller or method to read.</param>
    /// <returns>The route template, or <see langword="null"/> when the conventional route is used.</returns>
    /// <remarks>
    /// A template appears either as the argument of the verb attribute or as a route attribute of its own, and both
    /// say the same thing. Neither has a counterpart in a Screenplay, which says what an application is rather than
    /// where it answers.
    /// </remarks>
    public static string? RouteOf(ISymbol symbol) =>
        symbol.GetAttributes()
            .Where(_ => IsRouting(_.AttributeClass))
            .Select(_ => _.GetArgument(0) as string)
            .FirstOrDefault(_ => !string.IsNullOrWhiteSpace(_));

    /// <summary>
    /// Gets the routable methods a controller declares.
    /// </summary>
    /// <param name="type">The controller to read.</param>
    /// <returns>The methods, ordered so that the same controller always reads the same way.</returns>
    public static IEnumerable<IMethodSymbol> MethodsOf(INamedTypeSymbol type) =>
        type.GetMembers()
            .OfType<IMethodSymbol>()
            .Where(_ => _ is { MethodKind: MethodKind.Ordinary, IsStatic: false, DeclaredAccessibility: Accessibility.Public })
            .OrderBy(_ => _.ToDisplayString(), StringComparer.Ordinal);

    /// <summary>
    /// Determines whether a method carries an attribute of a given name from the MVC namespace.
    /// </summary>
    /// <param name="method">The method to check.</param>
    /// <param name="attributeName">The metadata name of the attribute.</param>
    /// <returns>True when the attribute is applied.</returns>
    static bool Carries(IMethodSymbol method, string attributeName) =>
        method.HasAttribute($"{MvcNamespace}.{attributeName}");

    /// <summary>
    /// Determines whether an attribute is one that says where something is served.
    /// </summary>
    /// <param name="attribute">The attribute to check.</param>
    /// <returns>True when the attribute carries a route template.</returns>
    static bool IsRouting(INamedTypeSymbol? attribute) =>
        attribute?.ContainingNamespace?.ToDisplayString() == MvcNamespace &&
        (attribute.Name == "RouteAttribute" || attribute.Name == "HttpGetAttribute" || Array.Exists(_mutating, verb => verb == attribute.Name));
}
