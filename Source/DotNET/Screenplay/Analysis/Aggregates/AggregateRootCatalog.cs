// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.CodeAnalysis;

namespace Cratis.Arc.Screenplay.Analysis.Aggregates;

/// <summary>
/// Keeps track of every aggregate root the application declares and of which of them a command reaches.
/// </summary>
/// <remarks>
/// What an aggregate root applies is stated through the command that calls it, so an aggregate root nothing calls is
/// the only one whose events go unstated. Reporting is therefore left until every command has been read, rather than
/// reporting each aggregate root the moment it is found.
/// <para>
/// An aggregate root is known by the assembly declaring it and its name rather than by its symbol. An application
/// written as several projects declares an aggregate root in one compilation and reaches it from another, and the
/// compilation reaching it sees it through a reference to the one declaring it. Whenever the two compilations bind
/// what they reference to different assembly symbols - which is up to the compiler and the references each project
/// carries - the reaching compilation sees a retargeted copy of the type, and a copy is never equal to the symbol
/// it copies.
/// </para>
/// </remarks>
public class AggregateRootCatalog
{
    readonly List<(AggregateRootIdentity Identity, INamedTypeSymbol Type, string Namespace)> _declared = [];
    readonly HashSet<AggregateRootIdentity> _reached = [];

    /// <summary>
    /// Records an aggregate root the application declares.
    /// </summary>
    /// <param name="type">The type declaring it.</param>
    /// <param name="namespace">The namespace it lives in.</param>
    public void Declare(INamedTypeSymbol type, string @namespace)
    {
        var identity = AggregateRootIdentity.Of(type);
        if (!_declared.Exists(declared => declared.Identity == identity))
        {
            _declared.Add((identity, type, @namespace));
        }
    }

    /// <summary>
    /// Records that a command handler reached an aggregate root.
    /// </summary>
    /// <param name="type">The aggregate root that was reached.</param>
    public void Reached(INamedTypeSymbol type) => _reached.Add(AggregateRootIdentity.Of(type));

    /// <summary>
    /// Reports every aggregate root whose events no command states.
    /// </summary>
    /// <param name="diagnostics">The diagnostics to report to.</param>
    public void Report(ScreenplayDiagnostics diagnostics)
    {
        foreach (var (_, type, @namespace) in _declared.Where(declared => !_reached.Contains(declared.Identity)))
        {
            diagnostics.Warning(
                ScreenplayDiagnosticCodes.AggregateRootWithoutCounterpart,
                $"'{type.Name}' is an aggregate root that no command in the compilation hands its work to, so the events it applies are not stated as produced by anything",
                @namespace);
        }
    }

    /// <summary>
    /// Identifies an aggregate root the same way in every compilation that sees it.
    /// </summary>
    /// <param name="Assembly">The identity of the assembly declaring it.</param>
    /// <param name="Name">Its fully qualified name.</param>
    readonly record struct AggregateRootIdentity(AssemblyIdentity Assembly, string Name)
    {
        public static AggregateRootIdentity Of(INamedTypeSymbol type) =>
            new(type.ContainingAssembly.Identity, type.OriginalDefinition.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat));
    }
}
