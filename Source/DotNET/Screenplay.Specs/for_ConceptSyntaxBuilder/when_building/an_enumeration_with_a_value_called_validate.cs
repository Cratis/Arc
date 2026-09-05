// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Screenplay.Model;
using Cratis.Screenplay.Syntax;

namespace Cratis.Arc.Screenplay.for_ConceptSyntaxBuilder.when_building;

/// <summary>
/// A concept body dispatches on <c>validate</c>, so an enumeration value called <c>Validate</c> is written on a line
/// of its own and read as the opening of an empty validate block. Nothing about that fails to compile, which is what
/// makes it worth reporting - the value disappears from the document without a word otherwise.
/// </summary>
public class an_enumeration_with_a_value_called_validate : given.a_concept_syntax_builder
{
    IEnumerable<ConceptSyntax> _result;

    void Because() => _result = _builder.Build(
        [new ConceptModel("RequestState", ScreenplayPrimitive.Enum, false, ["Pending", "Validate", "Approved"], [])]);

    [Fact] void should_leave_the_value_out() => _result.Single().Values.ShouldContainOnly(["pending", "approved"]);
    [Fact] void should_report_the_value() => _diagnostics.All.Select(_ => _.Code).ShouldContainOnly([ScreenplayDiagnosticCodes.NameReservedByGrammar]);
    [Fact] void should_locate_the_report_at_the_concept() => _diagnostics.All.Single().Location.ShouldEqual("RequestState");
    [Fact] void should_name_the_value_in_the_report() => _diagnostics.All.Single().Message.Contains("'Validate'", StringComparison.Ordinal).ShouldBeTrue();
    [Fact] void should_name_the_block_reserving_it_in_the_report() => _diagnostics.All.Single().Message.Contains("concept block", StringComparison.Ordinal).ShouldBeTrue();
}
