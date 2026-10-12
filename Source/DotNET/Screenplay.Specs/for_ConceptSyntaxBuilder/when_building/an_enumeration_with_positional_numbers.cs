// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Screenplay.Model;
using Cratis.Screenplay.Syntax;

namespace Cratis.Arc.Screenplay.for_ConceptSyntaxBuilder.when_building;

public class an_enumeration_with_positional_numbers : given.a_concept_syntax_builder
{
    ConceptSyntax _result;

    void Because() => _result = _builder.Build([new ConceptModel("Status", ScreenplayPrimitive.Enum, false, ["Draft", "Issued"], []) { EnumNumbers = [0, 1] }]).Single();

    [Fact] void should_pin_positional_numbers_too() => _result.ValueNumbers.ShouldEqual([0, 1]);
    [Fact] void should_report_nothing() => _diagnostics.All.ShouldBeEmpty();
}
