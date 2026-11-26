// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Screenplay.Emission.Naming;
using Cratis.Arc.Screenplay.Model;
using Cratis.Screenplay.Diagnostics;
using Cratis.Screenplay.Syntax;
using Cratis.Screenplay.Syntax.Captures;

namespace Cratis.Arc.Screenplay.Emission.Captures;

/// <summary>
/// Builds the Screenplay <c>capture</c> declaration for a stateful once-only capture.
/// </summary>
/// <param name="naming">The <see cref="IScreenplayNaming"/> used for name conversion.</param>
public class CaptureSyntaxBuilder(IScreenplayNaming naming)
{
    /// <summary>
    /// Builds the capture declaration.
    /// </summary>
    /// <param name="capture">The capture to build for.</param>
    /// <returns>The <see cref="CaptureSyntax"/>.</returns>
    public CaptureSyntax Build(CaptureModel capture)
    {
        var location = SourceLocation.Start;
        var settings = capture.SourceEvents
            .Select(_ => new CaptureSourceSettingSyntax(CaptureEventsSource.FromSetting, naming.ToDeclarationName(_), location))
            .ToList();
        var mappings = capture.Mappings
            .Select(_ => new PropertyMappingSyntax(naming.ToPropertyName(_.Property), new SourceItemExpressionSyntax(naming.ToPropertyName(_.Field), location), location))
            .ToList();

        return new(
            naming.ToDeclarationName(capture.Name),
            new CaptureSourceSyntax(CaptureEventsSource.Kind, settings, location),
            naming.ToPropertyName(capture.Key),
            [],
            [new CaptureAppendSyntax(naming.ToDeclarationName(capture.AppendedEvent), null, mappings, location)],
            [],
            [],
            location)
        {
            KeyIsEventSource = capture.KeyIsEventSource
        };
    }
}
