// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Screenplay.Emission.Naming;
using Cratis.Arc.Screenplay.Emission.Screens;
using Cratis.Arc.Screenplay.Emission.Types;

namespace Cratis.Arc.Screenplay.for_ScreenSyntaxBuilder.given;

public class a_screen_syntax_builder : Specification
{
    protected ScreenSyntaxBuilder _builder;

    void Establish()
    {
        var naming = new ScreenplayNaming();

        _builder = new(naming, new TypeReferenceConverter(naming));
    }
}
