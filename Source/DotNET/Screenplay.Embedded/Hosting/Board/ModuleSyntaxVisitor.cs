// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Syntax;

namespace Cratis.Arc.Screenplay.Embedded.Hosting.Board;

/// <summary>
/// Visits a Screenplay module and produces the module the board draws.
/// </summary>
/// <param name="documentId">The identifier of the document being read.</param>
/// <param name="owners">The events declared across the application.</param>
/// <param name="warnings">Where to report everything the board cannot hold.</param>
public class ModuleSyntaxVisitor(string documentId, ScreenplayEventOwners owners, EventModelWarnings warnings)
    : IModuleSyntaxVisitor<Module>
{
    int _sortOrder;

    /// <inheritdoc/>
    public Module Visit(ModuleSyntax syntax)
    {
        ArgumentNullException.ThrowIfNull(syntax);

        var featureVisitor = new FeatureSyntaxVisitor(documentId, syntax.Name, owners, warnings);

        warnings.Dropped(
            EventModelWarningCodes.SliceConstructNotCarried,
            syntax.Name,
            (syntax.Forms ?? []).Count() + (syntax.DialogTemplates ?? []).Count() + (syntax.ScreenTemplates ?? []).Count(),
            "form, dialog or screen template",
            "the board draws modules, features and slices, and holds no user experience of its own",
            syntax.Location);

        return new Module(
            DeterministicId.From(documentId, syntax.Name, "module"),
            syntax.Name,
            Features: [.. (syntax.Features ?? []).Select(featureVisitor.Visit)],
            Collapsed: false,
            SortOrder: _sortOrder++,
            CommentCount: 0);
    }
}
