// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Screenplay.Analysis.Validation;
using Cratis.Arc.Screenplay.Model;
using Microsoft.CodeAnalysis;

namespace Cratis.Arc.Screenplay.Analysis.Commands;

/// <summary>
/// Reads the model-bound commands a slice declares.
/// </summary>
/// <param name="properties">The <see cref="PropertyReader"/> reading the input of each command.</param>
/// <param name="produces">The <see cref="ProducesReader"/> reading what each command produces.</param>
/// <param name="validators">The <see cref="ValidatorCatalog"/> holding the rules declared for each command.</param>
/// <param name="paths">The <see cref="SourcePaths"/> rewriting the path of the file each command lives in.</param>
/// <remarks>
/// The input of a command is what the record itself declares. The parameters of its handler are infrastructure -
/// injected services and current state - and are never part of what a caller sends, which is exactly the mistake a
/// reflection-based reading of the handler signature makes.
/// </remarks>
public class CommandReader(
    PropertyReader properties,
    ProducesReader produces,
    ValidatorCatalog validators,
    SourcePaths paths)
{
    /// <summary>
    /// The name of the method handling a command.
    /// </summary>
    public const string HandleMethod = "Handle";

    /// <summary>
    /// Determines whether a type is a model-bound command.
    /// </summary>
    /// <param name="type">The type to check.</param>
    /// <returns>True when the type is a command.</returns>
    public static bool IsCommand(ITypeSymbol type) => type.HasAttribute(WellKnownTypeNames.CommandAttribute);

    /// <summary>
    /// Reads a command.
    /// </summary>
    /// <param name="type">The type declaring the command.</param>
    /// <param name="location">Where the command lives, for use in diagnostics.</param>
    /// <returns>The <see cref="CommandModel"/>.</returns>
    public CommandModel Read(INamedTypeSymbol type, string location)
    {
        var handlers = Handlers(type);

        return new(
            type.Name,
            Documentation.SummaryOf(type),
            properties.Read(type),
            AuthorizationReader.Read(type),
            validators.For(type),
            produces.Read(type, handlers, location),
            ConcurrencyReader.Read(type),
            paths.Relative(type.SourceFilePath()));
    }

    /// <summary>
    /// Gets the handler methods a command declares.
    /// </summary>
    /// <param name="type">The type declaring the command.</param>
    /// <returns>The handlers, ordered so that the same command always reads the same way.</returns>
    static IReadOnlyList<IMethodSymbol> Handlers(INamedTypeSymbol type) =>
    [
        .. type.GetMembers(HandleMethod)
            .OfType<IMethodSymbol>()
            .Where(_ => _ is { MethodKind: MethodKind.Ordinary, IsStatic: false })
            .OrderBy(_ => _.ToDisplayString(), StringComparer.Ordinal)
    ];
}
