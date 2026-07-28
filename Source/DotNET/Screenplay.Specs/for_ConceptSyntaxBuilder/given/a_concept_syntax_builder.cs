// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Screenplay.Emission.Concepts;
using Cratis.Arc.Screenplay.Emission.Naming;
using Cratis.Arc.Screenplay.Emission.Validation;

namespace Cratis.Arc.Screenplay.for_ConceptSyntaxBuilder.given;

public class a_concept_syntax_builder : Specification
{
    protected ConceptSyntaxBuilder _builder;
    protected ScreenplayDiagnostics _diagnostics;

    void Establish()
    {
        var naming = new ScreenplayNaming();
        _diagnostics = new();
        _builder = new(naming, new ValidationSyntaxBuilder(naming, _diagnostics), _diagnostics);
    }
}
