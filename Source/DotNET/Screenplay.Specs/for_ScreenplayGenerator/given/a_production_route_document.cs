// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Screenplay.for_ScreenplayGenerator.given;

/// <summary>
/// Verifies routed output including the pin's redundant-route advisory for explicit fan-out on the default route.
/// </summary>
public class a_production_route_document : a_generated_document
{
    /// <summary>
    /// Requires successful binding and stable syntax, allowing only the known redundant-route warning.
    /// </summary>
    protected void AssertRoutedDocument()
    {
        AssertPartialDocument();
        Bound.Success.ShouldBeTrue();

        // The pinned binder requires production Stream for a non-identifier destination even on the default route.
        // Removing the apparently redundant route would turn a bound fan-out into PLAY0504.
        RoundTrip.Diagnostics.Where(diagnostic => diagnostic.Severity == Cratis.Screenplay.Diagnostics.DiagnosticSeverity.Warning && diagnostic.Code != "PLAY0664").ShouldBeEmpty();
    }
}
