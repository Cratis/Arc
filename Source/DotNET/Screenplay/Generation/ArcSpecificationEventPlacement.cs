// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Screenplay.Analysis;
using Cratis.Arc.Screenplay.Analysis.Commands;
using Cratis.Screenplay.Generation.DotNet;
using Microsoft.CodeAnalysis;

namespace Cratis.Arc.Screenplay.Generation;

/// <summary>
/// Proves whether an event is produced by an exact model-bound command handler.
/// </summary>
static class ArcSpecificationEventPlacement
{
    /// <summary>
    /// Determines whether a command handler signature proves the event belongs to a StateChange slice.
    /// </summary>
    /// <param name="context">The analyzed source context.</param>
    /// <param name="eventType">The exact event type.</param>
    /// <returns><see langword="true"/> when one authored command handler returns the event.</returns>
    public static bool IsStateChangeEvent(DotNetAnalysisContext context, INamedTypeSymbol eventType) =>
        context.Projects
            .Where(project => project.Role == DotNetProjectRole.Application)
            .SelectMany(project => ArtifactCatalog.From(project.Compilation).Types)
            .Where(CommandReader.IsCommand)
            .SelectMany(command => command.GetMembers(CommandReader.HandleMethod)
                .OfType<IMethodSymbol>()
                .Where(method => method is { MethodKind: MethodKind.Ordinary, IsStatic: false }))
            .SelectMany(method => HandlerBodies.EventTypesIn(method.ReturnType))
            .Any(produced => SymbolEqualityComparer.Default.Equals(produced, eventType));
}
