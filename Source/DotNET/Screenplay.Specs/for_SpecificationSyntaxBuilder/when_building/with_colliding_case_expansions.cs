// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Screenplay.Analysis.Specifications;
using Cratis.Arc.Screenplay.Emission.Naming;
using Cratis.Arc.Screenplay.Emission.Specifications;
using Cratis.Arc.Screenplay.Model;
using Cratis.Screenplay.Syntax.Specifications;

namespace Cratis.Arc.Screenplay.for_SpecificationSyntaxBuilder.when_building;

public class with_colliding_case_expansions : Specification
{
    List<SpecificationSyntax> _syntax;
    readonly ScreenplayDiagnostics _diagnostics = new();

    void Because()
    {
        var action = new SpecificationStateModel("Register", SpecificationStateKind.Command, []);
        var first = new SpecificationModel("Jane", [], action, [], ["rejected"]);
        var second = first with { Name = "John" };
        var third = first with { Name = "Other" };
        var fourth = first with { Name = "Else" };
        new SpecificationCaseTable(first with { Name = "Registering" }, [("name", new("String", false, false))], [("Jane", first, []), ("John", second, [])]).Register();
        new SpecificationCaseTable(first with { Name = "Registering_Jane" }, [("name", new("String", false, false))], [("Other", third, []), ("Else", fourth, [])]).Register();
        var naming = Substitute.For<IScreenplayNaming>();
        naming.ToDeclarationName(Arg.Any<string>()).Returns(call => call.Arg<string>());
        naming.ToPropertyName(Arg.Any<string>()).Returns(call => call.Arg<string>());
        naming.ToStringLiteral(Arg.Any<string>()).Returns(call => call.Arg<string>());
        _syntax = new SpecificationSyntaxBuilder(naming) { Diagnostics = _diagnostics }.Build([first, second, third, fourth]).ToList();
    }

    [Fact] void should_leave_both_tables_as_individual_rows() => _syntax.Select(syntax => syntax.Name).ShouldContainOnly(["Jane", "John", "Other", "Else"]);
    [Fact] void should_not_emit_colliding_expanded_names() => _syntax.SelectMany(syntax => syntax.Cases ?? []).ShouldBeEmpty();
    [Fact] void should_explain_the_collision() => _diagnostics.All.Count(diagnostic => diagnostic.Code == ScreenplayDiagnosticCodes.SpecificationCasesNotFolded).ShouldEqual(4);
}
