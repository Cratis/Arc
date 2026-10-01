// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Syntax;

namespace Cratis.Arc.Screenplay.Embedded.Hosting.Board;

/// <summary>
/// Visits a Screenplay feature - and, recursively, its sub-features - producing the feature tree the board draws.
/// </summary>
/// <param name="documentId">The identifier of the document being read.</param>
/// <param name="path">The path to the module or feature holding this one.</param>
/// <param name="owners">The events declared across the application.</param>
/// <param name="warnings">Where to report everything the board cannot hold.</param>
public class FeatureSyntaxVisitor(string documentId, string path, ScreenplayEventOwners owners, EventModelWarnings warnings)
    : IFeatureSyntaxVisitor<Feature>
{
    /// <inheritdoc/>
    public Feature Visit(FeatureSyntax syntax)
    {
        ArgumentNullException.ThrowIfNull(syntax);

        var featurePath = $"{path}/{syntax.Name}";
        var sliceVisitor = new SliceSyntaxVisitor(documentId, featurePath, owners, warnings);

        return new Feature(
            DeterministicId.From(documentId, featurePath, "feature"),
            syntax.Name,
            SubFeatures: [.. (syntax.Features ?? []).Select(new FeatureSyntaxVisitor(documentId, featurePath, owners, warnings).Visit)],
            Slices: [.. (syntax.Slices ?? []).Select(sliceVisitor.Visit)],
            Collapsed: false,
            RowCollapsed: false,
            Enabled: true,
            CommentCount: 0);
    }
}
