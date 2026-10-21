// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Screenplay.Model;
using Cratis.Screenplay.Diagnostics;
using Cratis.Screenplay.Syntax;

namespace Cratis.Arc.Screenplay.Emission.Screens;

/// <summary>
/// Writes literal parameter bindings shared by navigation directives and toolbar navigation items.
/// </summary>
internal static class ScreenNavigationSyntax
{
    /// <summary>
    /// Builds one literal binding per named route segment, preserving its router parameter name.
    /// </summary>
    /// <param name="navigation">The resolved literal gesture.</param>
    /// <returns>The parameter syntax.</returns>
    internal static IEnumerable<ScreenNavigationParameterSyntax> Parameters(ScreenNavigationModel navigation) => navigation.Parameters
        .Select(parameter => new ScreenNavigationParameterSyntax(
            parameter.Key,
            new UiBindingSyntax(UiBindingKind.Literal, string.Empty, SourceLocation.Start)
            {
                Literal = new LiteralExpressionSyntax(parameter.Value, SourceLocation.Start)
            },
            SourceLocation.Start));
}
