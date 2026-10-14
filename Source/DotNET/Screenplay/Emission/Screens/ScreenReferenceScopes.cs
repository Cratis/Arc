// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Screenplay.Emission.Naming;
using Cratis.Arc.Screenplay.Emission.Slices;
using Cratis.Screenplay.Syntax;

namespace Cratis.Arc.Screenplay.Emission.Screens;

/// <summary>
/// Qualifies screen references against the declarations and scopes of the emitted slice tree.
/// </summary>
/// <param name="slices">The placed slices and their emitted scopes.</param>
/// <param name="naming">The declaration naming convention.</param>
/// <param name="diagnostics">The reporter for references whose emitted owner cannot be resolved.</param>
public class ScreenReferenceScopes(IReadOnlyList<(PlacedSlice Placed, string[] Scope)> slices, IScreenplayNaming naming, ScreenplayDiagnostics diagnostics)
{
    enum ReferenceKind
    {
        Command = 0,
        Query = 1,
        Screen = 2
    }

    /// <summary>
    /// Replaces owner namespaces with the shortest unambiguous trailing scope, omitting unresolved references.
    /// </summary>
    /// <returns>The placed slices with qualified screen directives.</returns>
    public IEnumerable<PlacedSlice> Apply() => slices.Select(entry => entry.Placed with
    {
        Slice = entry.Placed.Slice with
        {
            Screens = entry.Placed.Slice.Screens.Select(screen => screen with
            {
                Directives = screen.Directives.Select(directive => Directive(directive, entry.Placed.Namespace, screen.Name))
                    .OfType<ScreenDirectiveSyntax>().ToList()
            }).ToList()
        }
    });

    static int DeclarationCount(SliceSyntax slice, string name, ReferenceKind kind) => kind switch
    {
        ReferenceKind.Query => slice.Queries.Count(declaration => declaration.Name == name),
        ReferenceKind.Screen => slice.Screens.Count(declaration => declaration.Name == name),
        _ => slice.Commands.Count(declaration => declaration.Name == name)
    };

    static bool EndsWith(string[] scope, string[] suffix) => scope.Length >= suffix.Length && scope.AsSpan(scope.Length - suffix.Length).SequenceEqual(suffix.AsSpan());

    ScreenDirectiveSyntax? Directive(ScreenDirectiveSyntax directive, string @namespace, string screen) => directive switch
    {
        ScreenActionSyntax action => Reference(action.Command, ReferenceKind.Command, @namespace, screen) is { } command
            ? action with { Command = command, Navigate = Navigate(action.Navigate, @namespace, screen) } : null,
        ScreenDataSyntax data => Reference(data.Query, ReferenceKind.Query, @namespace, screen) is { } query ? data with { Query = query } : null,
        ScreenToolbarSyntax toolbar => Toolbar(toolbar, @namespace, screen),
        ScreenNavigateSyntax navigate => Navigate(navigate, @namespace, screen),
        _ => directive
    };

    ScreenToolbarSyntax? Toolbar(ScreenToolbarSyntax toolbar, string @namespace, string screen)
    {
        var items = toolbar.Items.Select(item => item.Kind is not (ToolbarItemKind.Action or ToolbarItemKind.Navigate) ? item :
            Reference(item.Target, item.Kind == ToolbarItemKind.Navigate ? ReferenceKind.Screen : ReferenceKind.Command, @namespace, screen) is { } target
                ? item with { Target = target } : null)
            .OfType<ToolbarItemSyntax>().ToList();

        return items.Count == 0 ? null : toolbar with { Items = items };
    }

    ScreenNavigateSyntax? Navigate(ScreenNavigateSyntax? navigate, string @namespace, string screen) => navigate is not null &&
        Reference(navigate.Screen, ReferenceKind.Screen, @namespace, screen) is { } target ? navigate with { Screen = target } : null;

    string? Reference(string reference, ReferenceKind kind, string @namespace, string screen)
    {
        var separator = reference.LastIndexOf('.');
        var owner = separator < 0 ? Namespace(@namespace) : reference[..separator];
        var name = separator < 0 ? reference : reference[(separator + 1)..];
        var owners = slices.Where(entry => Namespace(entry.Placed.Namespace) == owner).ToList();
        var scope = owners.Count == 1 && DeclarationCount(owners[0].Placed.Slice, name, kind) == 1 ? owners[0].Scope : null;
        var qualified = scope is null ? null : separator < 0 ? reference : Qualify(scope, name, kind);
        if (qualified is null)
        {
            diagnostics.Information(
                ScreenplayDiagnosticCodes.ScreenStructureNotInferred,
                $"The screen '{screen}' references '{reference}', whose emitted declaration cannot be resolved unambiguously, so the binding is left out",
                @namespace);
        }

        return qualified;
    }

    string? Qualify(string[] scope, string name, ReferenceKind kind)
    {
        for (var length = 1; length <= scope.Length; length++)
        {
            var suffix = scope[^length..];
            if (slices.Where(entry => EndsWith(entry.Scope, suffix)).Sum(entry => DeclarationCount(entry.Placed.Slice, name, kind)) == 1)
            {
                return string.Join('.', suffix.Append(name));
            }
        }

        return null;
    }

    string Namespace(string value) => string.Join('.', value.Split('.').Select(naming.ToDeclarationName));
}
