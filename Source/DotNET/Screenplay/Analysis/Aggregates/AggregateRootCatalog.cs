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
/// </remarks>
public class AggregateRootCatalog
{
    readonly List<(INamedTypeSymbol Type, string Namespace)> _declared = [];
    readonly HashSet<INamedTypeSymbol> _reached = new(SymbolEqualityComparer.Default);

    /// <summary>
    /// Records an aggregate root the application declares.
    /// </summary>
    /// <param name="type">The type declaring it.</param>
    /// <param name="namespace">The namespace it lives in.</param>
    public void Declare(INamedTypeSymbol type, string @namespace)
    {
        if (!_declared.Exists(declared => SymbolEqualityComparer.Default.Equals(declared.Type, type)))
        {
            _declared.Add((type, @namespace));
        }
    }

    /// <summary>
    /// Records that a command handler reached an aggregate root.
    /// </summary>
    /// <param name="type">The aggregate root that was reached.</param>
    public void Reached(INamedTypeSymbol type) => _reached.Add(type);

    /// <summary>
    /// Reports every aggregate root whose events no command states.
    /// </summary>
    /// <param name="diagnostics">The diagnostics to report to.</param>
    public void Report(ScreenplayDiagnostics diagnostics)
    {
        foreach (var (type, @namespace) in _declared.Where(declared => !_reached.Contains(declared.Type)))
        {
            diagnostics.Warning(
                ScreenplayDiagnosticCodes.AggregateRootWithoutCounterpart,
                $"'{type.Name}' is an aggregate root that no command in the compilation hands its work to, so the events it applies are not stated as produced by anything",
                @namespace);
        }
    }
}
