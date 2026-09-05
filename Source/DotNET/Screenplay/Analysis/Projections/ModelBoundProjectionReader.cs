// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Screenplay.Model;
using Microsoft.CodeAnalysis;

namespace Cratis.Arc.Screenplay.Analysis.Projections;

/// <summary>
/// Reads the projection a model-bound read model declares through attributes.
/// </summary>
/// <param name="diagnostics">The <see cref="ScreenplayDiagnostics"/> anything unmappable is reported to.</param>
/// <remarks>
/// Automatic property mapping is on unless the read model turns it off, which is the opposite of what a fluent
/// projection defaults to, so it is stated explicitly rather than inherited.
/// </remarks>
public class ModelBoundProjectionReader(ScreenplayDiagnostics diagnostics)
{
    /// <summary>
    /// The identifier of the sequence a projection observes unless it says otherwise.
    /// </summary>
    public const string EventLogSequence = "event-log";

    readonly ModelBoundScopeReader _scopes = new(diagnostics);

    /// <summary>
    /// Reads the projection a read model declares.
    /// </summary>
    /// <param name="readModel">The read model to read.</param>
    /// <param name="location">Where the read model lives, for use in diagnostics.</param>
    /// <returns>The <see cref="ProjectionModel"/>, or <see langword="null"/> when the read model declares none.</returns>
    public ProjectionModel? Read(INamedTypeSymbol readModel, string location)
    {
        var scope = _scopes.Read(readModel, location);
        if (ModelBoundScopeReader.IsEmpty(scope))
        {
            return null;
        }

        var attribute = readModel.GetAttribute(WellKnownTypeNames.ProjectionAttribute);

        return new(
            attribute?.GetArgument(0) as string is { Length: > 0 } id ? id : readModel.ToDisplayString(),
            readModel.Name,
            attribute?.GetArgument(1) as string ?? EventLogSequence,
            readModel.HasAttribute(ModelBoundNames.NoAutoMap) ? ProjectionAutoMapMode.Disabled : ProjectionAutoMapMode.Enabled,
            readModel.HasAttribute(ModelBoundNames.FromAll),
            scope);
    }
}
