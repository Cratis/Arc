// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Screenplay.Model;
using Cratis.Screenplay.Syntax;

namespace Cratis.Arc.Screenplay.for_ConceptSyntaxBuilder.when_building;

public class an_enumeration_with_colliding_names : given.a_concept_syntax_builder
{
    ConceptSyntax _result;

    void Because() => _result = _builder.Build([new ConceptModel("Status", ScreenplayPrimitive.Enum, false, ["Draft", "draft", "Issued"], []) { EnumNumbers = [0, 1, 2] }]).Single();

    [Fact] void should_keep_the_first_name() => _result.Values.ShouldEqual(["draft", "issued"]);
    [Fact] void should_not_renumber_the_surviving_members() => _result.ValueNumbers.ShouldEqual([0, 2]);
    [Fact] void should_report_the_lost_member_identity() => _diagnostics.All.Single().Code.ShouldEqual(ScreenplayDiagnosticCodes.EnumMemberNumbersNotRepresentable);
    [Fact] void should_locate_the_loss() => _diagnostics.All.Single().Location.ShouldEqual("Status");
}
