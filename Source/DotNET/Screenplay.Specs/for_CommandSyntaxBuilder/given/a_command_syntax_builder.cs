// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Screenplay.Emission.Commands;
using Cratis.Arc.Screenplay.Emission.Naming;
using Cratis.Arc.Screenplay.Emission.Policies;
using Cratis.Arc.Screenplay.Emission.Types;
using Cratis.Arc.Screenplay.Emission.Validation;

namespace Cratis.Arc.Screenplay.for_CommandSyntaxBuilder.given;

public class a_command_syntax_builder : Specification
{
    protected CommandSyntaxBuilder _builder;
    protected ScreenplayDiagnostics _diagnostics;

    void Establish()
    {
        var naming = new ScreenplayNaming();
        _diagnostics = new();
        var names = new NameAvailability(naming, _diagnostics);
        _builder = new(
            naming,
            new TypeReferenceConverter(naming),
            new AuthorizeSyntaxBuilder(),
            new ValidationSyntaxBuilder(naming, _diagnostics),
            new ProducesSyntaxBuilder(naming, names),
            new ConcurrencySyntaxBuilder(naming, _diagnostics),
            names);
    }
}
