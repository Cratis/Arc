// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.RegularExpressions;
using Cratis.Arc.Screenplay.Emission.Naming;
using Cratis.Arc.Screenplay.Model;
using Cratis.Screenplay.Diagnostics;
using Cratis.Screenplay.Syntax.Projections;

namespace Cratis.Arc.Screenplay.Emission.Projections;

/// <summary>
/// Builds the Screenplay <c>projection</c> declaration for a projection.
/// </summary>
/// <param name="naming">The <see cref="IScreenplayNaming"/> used for name conversion.</param>
/// <param name="diagnostics">The <see cref="ScreenplayDiagnostics"/> anything unmappable is reported to.</param>
/// <param name="names">The <see cref="NameAvailability"/> deciding which properties a block can map onto.</param>
public partial class ProjectionSyntaxBuilder(IScreenplayNaming naming, ScreenplayDiagnostics diagnostics, NameAvailability names)
{
    /// <summary>
    /// The identifier of the default event sequence, which is never written out.
    /// </summary>
    public const string EventLogSequence = "event-log";

    /// <summary>
    /// Builds the projection declaration.
    /// </summary>
    /// <param name="projection">The projection to build for.</param>
    /// <param name="location">Where the projection lives, for use in diagnostics.</param>
    /// <returns>The <see cref="ProjectionSyntax"/>, or <see langword="null"/> when there is nothing to declare.</returns>
    public ProjectionSyntax? Build(ProjectionModel projection, string location)
    {
        var readModel = naming.ToDeclarationName(projection.ReadModel);
        var blocks = new ProjectionBlockConverter(naming, readModel, diagnostics, location, names)
            .Convert(projection.Scope, projection.SubscribesToAllEvents)
            .ToList();

        if (blocks.Count == 0)
        {
            // A projection with no directives at all does not compile - leaving it out keeps the document valid.
            diagnostics.Warning(
                ScreenplayDiagnosticCodes.EmptyProjection,
                $"The projection '{projection.Identifier}' declares nothing that can be expressed and was left out",
                location);

            return null;
        }

        return new(
            GetName(projection, readModel),
            readModel.Length <= 1 ? null : readModel,
            GetSequence(projection),
            ProjectionBlockConverter.ToAutoMapMode(projection.AutoMap),
            null,
            blocks,
            SourceLocation.Start);
    }

    /// <summary>
    /// Gets the sequence to write out, if it is both non default and expressible as an identifier.
    /// </summary>
    /// <param name="projection">The projection to read from.</param>
    /// <returns>The sequence identifier, or <see langword="null"/>.</returns>
    static string? GetSequence(ProjectionModel projection)
    {
        var sequence = projection.EventSequenceId;

        return string.IsNullOrEmpty(sequence) ||
            string.Equals(sequence, EventLogSequence, StringComparison.Ordinal) ||
            !Identifier().IsMatch(sequence)
                ? null
                : sequence;
    }

    /// <summary>
    /// Gets the pattern an identifier has to match.
    /// </summary>
    /// <returns>The compiled regular expression.</returns>
    [GeneratedRegex(@"^[A-Za-z_]\w*$", RegexOptions.None, matchTimeoutMilliseconds: 1000)]
    private static partial Regex Identifier();

    /// <summary>
    /// Gets the name of the projection from its identifier, falling back to the read model.
    /// </summary>
    /// <param name="projection">The projection to name.</param>
    /// <param name="readModel">The sanitized name of the read model the projection builds.</param>
    /// <returns>The projection name.</returns>
    string GetName(ProjectionModel projection, string readModel)
    {
        var identifier = projection.Identifier ?? string.Empty;
        var lastSegment = identifier[(identifier.LastIndexOf('.') + 1)..];
        var name = naming.ToDeclarationName(lastSegment);

        return name.Length <= 1 ? readModel : name;
    }
}
