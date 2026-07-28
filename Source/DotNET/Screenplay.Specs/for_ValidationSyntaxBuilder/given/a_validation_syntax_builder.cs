// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Screenplay.Emission.Naming;
using Cratis.Arc.Screenplay.Emission.Validation;

namespace Cratis.Arc.Screenplay.for_ValidationSyntaxBuilder.given;

public class a_validation_syntax_builder : Specification
{
    protected ValidationSyntaxBuilder _builder;
    protected ScreenplayDiagnostics _diagnostics;

    void Establish()
    {
        _diagnostics = new();
        _builder = new(new ScreenplayNaming(), _diagnostics);
    }
}
