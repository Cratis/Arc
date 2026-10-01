// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Syntax;

namespace Cratis.Arc.Screenplay.Embedded.Hosting.Board;

/// <summary>
/// Visits a whole Screenplay application and produces the document the board draws.
/// </summary>
/// <param name="documentId">The identifier of the document being read - every identity is derived from it.</param>
/// <param name="name">The name to give the document when the source names no domain.</param>
/// <remarks>
/// This is the visitor the Screenplay compiler hands the compiled application to. Screenplay has no notion
/// of a module collection, so every module lands in a single one at origin, and the board lays the rest out
/// from nesting and sort order.
/// </remarks>
public class EventModelSyntaxVisitor(string documentId, string name) : IApplicationSyntaxVisitor<EventModel>
{
    /// <summary>
    /// Gets everything the board could not hold, found while visiting.
    /// </summary>
    public EventModelWarnings Warnings { get; } = new();

    /// <inheritdoc/>
    public EventModel Visit(ApplicationSyntax syntax)
    {
        ArgumentNullException.ThrowIfNull(syntax);

        var moduleVisitor = new ModuleSyntaxVisitor(documentId, ScreenplayEventOwners.From(documentId, syntax), Warnings);
        var modules = (syntax.Modules ?? []).Select(moduleVisitor.Visit).ToList();

        WarnForApplication(syntax);

        return new EventModel(
            DeterministicId.From(documentId, "eventModel"),
            string.IsNullOrWhiteSpace(syntax.Domain?.Name) ? name : syntax.Domain.Name,
            Collections:
            [
                new ModuleCollection(
                    DeterministicId.From(documentId, "collection"),
                    Position.Origin,
                    modules,
                    Actors: [])
            ],
            StickyNotes: [],
            Links: []);
    }

    void WarnForApplication(ApplicationSyntax syntax)
    {
        List<string> declarations =
        [
            .. (syntax.Concepts ?? []).Any() ? new[] { "concepts" } : [],
            .. (syntax.Types ?? []).Any() ? new[] { "types" } : [],
            .. (syntax.Policies ?? []).Any() ? new[] { "policies" } : [],
            .. (syntax.Personas ?? []).Any() ? new[] { "personas" } : [],
            .. (syntax.Seeds ?? []).Any() ? new[] { "seeds" } : [],
            .. (syntax.Imports ?? []).Any() ? new[] { "imports" } : [],
            .. (syntax.Triggers ?? []).Any() ? new[] { "triggers" } : [],
            .. (syntax.UiProfiles ?? []).Any() ? new[] { "ui profiles" } : [],
            .. (syntax.Themes ?? []).Any() ? new[] { "themes" } : [],
            .. (syntax.Layouts ?? []).Any() ? new[] { "layouts" } : [],
            .. syntax.Authentication is not null ? new[] { "an authentication scheme" } : []
        ];

        Warnings.Present(
            EventModelWarningCodes.ApplicationDeclarationNotCarried,
            declarations.Count > 0,
            name,
            $"the document declares {string.Join(", ", declarations)} at the application level, and the board draws modules, features and slices only",
            syntax.Location);
    }
}
