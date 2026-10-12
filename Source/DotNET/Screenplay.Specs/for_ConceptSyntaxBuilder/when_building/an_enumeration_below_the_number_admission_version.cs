// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Screenplay.Emission.Concepts;
using Cratis.Arc.Screenplay.Emission.Naming;
using Cratis.Arc.Screenplay.Emission.Validation;
using Cratis.Arc.Screenplay.Model;
using Cratis.Screenplay.Semantics;
using Cratis.Screenplay.Syntax;

namespace Cratis.Arc.Screenplay.for_ConceptSyntaxBuilder.when_building;

public class an_enumeration_below_the_number_admission_version : given.a_concept_syntax_builder
{
    ConceptSyntax _result;

    void Establish()
    {
        var naming = new ScreenplayNaming();
        _builder = new ConceptSyntaxBuilder(naming, new ValidationSyntaxBuilder(naming, _diagnostics), _diagnostics, new NameAvailability(naming, _diagnostics)) { MaximumExecutableModelVersion = SemanticVersion.V11 };
    }

    void Because() => _result = _builder.Build([new ConceptModel("Status", ScreenplayPrimitive.Enum, false, ["Draft", "Issued"], []) { EnumNumbers = [1, 4] }]).Single();

    [Fact] void should_retain_positional_output() => _result.ValueNumbers.ShouldBeNull();
    [Fact] void should_report_the_cap() => _diagnostics.All.Single().Message.ShouldContain("cap v11.0");
    [Fact] void should_report_lost_identity() => _diagnostics.All.Single().Code.ShouldEqual(ScreenplayDiagnosticCodes.EnumMemberNumbersNotRepresentable);
    [Fact] void should_name_the_numbers_it_withheld() => _diagnostics.All.Single().Message.ShouldContain("draft=1, issued=4");
}
