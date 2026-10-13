// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Screenplay.Analysis.Commands;
using Cratis.Arc.Screenplay.Analysis.Events;
using Cratis.Arc.Screenplay.Model;
using Microsoft.CodeAnalysis;

namespace Cratis.Arc.Screenplay.Analysis.Projections;

/// <summary>
/// Reads what a reducer can be said about, and reports what it cannot.
/// </summary>
/// <param name="diagnostics">The <see cref="ScreenplayDiagnostics"/> anything unmappable is reported to.</param>
/// <remarks>
/// A reducer folds events into a read model with code, and Screenplay has no construct for a fold. The events it
/// observes and the read model it builds are still worth stating, so a projection over the provable events is
/// recovered. Runtime-only event coverage and the fold itself are reported as lost rather than silently invented.
/// </remarks>
public class ReducerReader(ScreenplayDiagnostics diagnostics)
{
    /// <summary>
    /// The identifier of the sequence a reducer observes unless it says otherwise.
    /// </summary>
    public const string EventLogSequence = "event-log";

    /// <summary>
    /// Determines whether a type is a reducer, and for what read model.
    /// </summary>
    /// <param name="type">The type to check.</param>
    /// <returns>The read model, or <see langword="null"/> when the type is not a reducer.</returns>
    public static ITypeSymbol? ReadModelOf(ITypeSymbol type) =>
        type is { IsAbstract: false, TypeKind: TypeKind.Class }
            ? type.FindInterface(WellKnownTypeNames.ReducerFor)?.TypeArguments[0] ??
                (type is INamedTypeSymbol model && SelfReducerMethods.Of(model).Any() ? model : null)
            : null;

    /// <summary>
    /// Reads the events a reducer observes.
    /// </summary>
    /// <param name="type">The type declaring the reducer.</param>
    /// <param name="readModel">The read model the reducer builds.</param>
    /// <param name="location">Where the reducer lives, for use in diagnostics.</param>
    /// <returns>The <see cref="ProjectionModel"/>, or <see langword="null"/> when it has no recognized event handlers.</returns>
    public ProjectionModel? Read(INamedTypeSymbol type, ITypeSymbol readModel, string location)
    {
        var methods = type.FindInterface(WellKnownTypeNames.ReducerFor) is not null
            ? type.GetMembers().OfType<IMethodSymbol>().Where(_ =>
                _ is { MethodKind: MethodKind.Ordinary, IsStatic: false, DeclaredAccessibility: Accessibility.Public } &&
                _.Parameters.Length > 0 && EventReader.IsEvent(_.Parameters[0].Type))
            : SelfReducerMethods.Of(type);
        var parameters = methods.Select(_ => _.Parameters[0].Type).ToArray();
        var observed = parameters
            .Where(EventReader.IsEvent)
            .Select(_ => _.Name)
            .Distinct(StringComparer.Ordinal)
            .Order(StringComparer.Ordinal)
            .ToList();

        if (parameters.Length == 0)
        {
            return null;
        }

        var incomplete = ReportIncompleteCoverage(type, parameters, location);
        var coverage = incomplete ? "events source analysis can prove" : "events it observes";
        diagnostics.Warning(
            ScreenplayDiagnosticCodes.ReducerWithoutCounterpart,
            $"'{type.Name}' folds events into '{readModel.Name}' with code, and what that code works out cannot be stated, so it is carried as a projection over the {coverage} and nothing is said of the fold",
            location);

        return new(
            type.ToDisplayString(),
            readModel.Name,
            EventLogSequence,
            ProjectionAutoMapMode.Enabled,
            false,
            ProjectionScopeModel.Empty with { From = [.. observed.Select(_ => new ProjectionFromModel([_], null, null, EmptyMap()))] },
            EventSourceReader.ReadObserved(type));
    }

    /// <summary>
    /// Gets a property map declaring nothing, which is all a fold can be said to map.
    /// </summary>
    /// <returns>The empty map.</returns>
    static Dictionary<string, string> EmptyMap() => new(StringComparer.Ordinal);

    bool ReportIncompleteCoverage(INamedTypeSymbol type, IEnumerable<ITypeSymbol> parameters, string location)
    {
        var unproven = parameters.Where(_ => !EventReader.IsEvent(_))
            .Select(_ => _.ToDisplayString())
            .Distinct(StringComparer.Ordinal)
            .Order(StringComparer.Ordinal)
            .ToArray();
        if (unproven.Length == 0)
        {
            return false;
        }

        diagnostics.Warning(
            ScreenplayDiagnosticCodes.ReducerWithoutCounterpart,
            $"'{type.Name}' accepts runtime-registered events through {string.Join(", ", unproven)}, whose registered concrete event types cannot be determined from source, so its observed-event coverage is incomplete; only provable events are listed",
            location);

        return true;
    }
}
