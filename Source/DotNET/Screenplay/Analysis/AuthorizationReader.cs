// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Screenplay.Model;
using Microsoft.CodeAnalysis;

namespace Cratis.Arc.Screenplay.Analysis;

/// <summary>
/// Reads what an artifact requires of the caller.
/// </summary>
/// <remarks>
/// Roles are declared in two shapes - the constructor form <c>[Roles("Librarian")]</c> and the named argument form
/// <c>[Authorize(Roles = "Librarian")]</c> - and both are read. Reading only one of them silently drops half the
/// authorization in an application, which is a bug the proxy generator has and this deliberately does not repeat.
/// </remarks>
public static class AuthorizationReader
{
    /// <summary>
    /// The name of the property carrying a comma separated list of roles.
    /// </summary>
    public const string RolesProperty = "Roles";

    /// <summary>
    /// Reads what an artifact and the type declaring it require of the caller.
    /// </summary>
    /// <param name="symbol">The artifact to read.</param>
    /// <param name="declaring">The type declaring it, when the artifact is a member.</param>
    /// <returns>The <see cref="AuthorizationModel"/>, or <see langword="null"/> when nothing is required.</returns>
    public static AuthorizationModel? Read(ISymbol symbol, ISymbol? declaring = null)
    {
        if (symbol.HasAttribute(WellKnownTypeNames.AllowAnonymousAttribute))
        {
            return null;
        }

        var attributes = Attributes(symbol).Concat(declaring is null ? [] : Attributes(declaring)).ToList();
        if (attributes.Count == 0)
        {
            return null;
        }

        var roles = attributes
            .SelectMany(Roles)
            .Where(_ => _.Length > 0)
            .Distinct(StringComparer.Ordinal)
            .Order(StringComparer.Ordinal)
            .ToList();

        return new(true, roles);
    }

    /// <summary>
    /// Gets every authorize attribute applied to a symbol, including the roles form deriving from it.
    /// </summary>
    /// <param name="symbol">The symbol to read.</param>
    /// <returns>The attributes.</returns>
    static IEnumerable<AttributeData> Attributes(ISymbol symbol) =>
        symbol.GetAttributes().Where(_ =>
            _.AttributeClass.Is(WellKnownTypeNames.AuthorizeAttribute) ||
            _.AttributeClass.Is(WellKnownTypeNames.RolesAttribute) ||
            _.AttributeClass.FindBase(WellKnownTypeNames.AuthorizeAttribute) is not null);

    /// <summary>
    /// Gets the roles an authorize attribute names, in either shape it can be written.
    /// </summary>
    /// <param name="attribute">The attribute to read.</param>
    /// <returns>The role names.</returns>
    static IEnumerable<string> Roles(AttributeData attribute)
    {
        var named = attribute.GetNamedArgument(RolesProperty) as string;
        var positional = attribute.ConstructorArguments
            .SelectMany(_ => _.Kind == TypedConstantKind.Array ? _.Values.Select(value => value.Value) : [_.Value])
            .OfType<string>();

        return Split(named).Concat(positional.SelectMany(Split));
    }

    /// <summary>
    /// Splits a comma separated list of roles.
    /// </summary>
    /// <param name="value">The value to split.</param>
    /// <returns>The role names.</returns>
    static IEnumerable<string> Split(string? value) =>
        string.IsNullOrWhiteSpace(value)
            ? []
            : value.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
}
