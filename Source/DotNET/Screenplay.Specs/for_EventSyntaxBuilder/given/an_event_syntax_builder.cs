// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Screenplay.Emission.Events;
using Cratis.Arc.Screenplay.Emission.Naming;
using Cratis.Arc.Screenplay.Emission.Types;

namespace Cratis.Arc.Screenplay.for_EventSyntaxBuilder.given;

public class an_event_syntax_builder : Specification
{
    protected EventSyntaxBuilder _builder;
    protected ScreenplayDiagnostics _diagnostics;

    void Establish()
    {
        var naming = new ScreenplayNaming();
        _diagnostics = new();
        _builder = new(naming, new TypeReferenceConverter(naming), new NameAvailability(naming, _diagnostics));
    }
}
