// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Numerics;
using Cratis.Arc.Screenplay.Model;
using Cratis.Screenplay.Syntax;

namespace Cratis.Arc.Screenplay.for_ConceptSyntaxBuilder.when_building;

public class an_enumeration_outside_int32 : given.a_concept_syntax_builder
{
    ConceptSyntax _result;

    void Because() => _result = _builder.Build([new ConceptModel("Status", ScreenplayPrimitive.Enum, false, ["Draft", "Issued"], []) { EnumNumbers = [0, new BigInteger(ulong.MaxValue)] }]).Single();

    [Fact] void should_withhold_all_numbers() => _result.ValueNumbers.ShouldBeNull();
    [Fact] void should_keep_the_names() => _result.Values.ShouldEqual(["draft", "issued"]);
    [Fact] void should_report_the_range() => _diagnostics.All.Single().Message.ShouldContain("outside int32");
    [Fact] void should_report_lost_identity() => _diagnostics.All.Single().Code.ShouldEqual(ScreenplayDiagnosticCodes.EnumMemberNumbersNotRepresentable);
    [Fact] void should_report_the_exact_number() => _diagnostics.All.Single().Message.ShouldContain("18446744073709551615");
}
