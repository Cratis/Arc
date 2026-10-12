// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Screenplay.Model;
using Cratis.Screenplay.Syntax;

namespace Cratis.Arc.Screenplay.for_ConceptSyntaxBuilder.when_building;

public class an_enumeration_with_reordered_numbers : given.a_concept_syntax_builder
{
    ConceptSyntax _result;

    void Because() => _result = _builder.Build([new ConceptModel("Status", ScreenplayPrimitive.Enum, false, ["Issued", "Draft"], []) { EnumNumbers = [4, 1] }]).Single();

    [Fact] void should_keep_declaration_order() => _result.Values.ShouldEqual(["issued", "draft"]);
    [Fact] void should_keep_numbers_with_their_names() => _result.ValueNumbers.ShouldEqual([4, 1]);
}
