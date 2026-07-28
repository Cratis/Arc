// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Screenplay.Model;
using Cratis.Screenplay.Syntax;
using ModelRuleKind = Cratis.Arc.Screenplay.Model.ValidationRuleKind;

namespace Cratis.Arc.Screenplay.for_ValidationSyntaxBuilder.when_building;

/// <summary>
/// A rule operand is a host expression, so a comparison against another property is written as the path naming it -
/// <c>endDate &gt;= startDate</c>. Written as a literal instead it would read as a comparison against the word
/// <c>startDate</c>, which is a document stating something the application does not do.
/// </summary>
public class a_rule_comparing_against_another_property : given.a_validation_syntax_builder
{
    IEnumerable<ValidationRuleSyntax> _rules;

    void Because() => _rules = _builder
        .Build(
            [new("EndDate", ModelRuleKind.GreaterThanOrEqual, new PropertyPathSource("StartDate"), null)],
            "Library.Lending.Reserving")
        .OfType<DeclarativeValidateSyntax>()
        .SelectMany(_ => _.Rules);

    [Fact] void should_state_the_rule() => _rules.Count().ShouldEqual(1);
    [Fact] void should_write_the_operand_as_a_path() => _rules.Single().Value.ShouldBeOfExactType<PathExpressionSyntax>();
    [Fact] void should_write_it_the_way_a_property_is_named() => ((PathExpressionSyntax)_rules.Single().Value!).Path.ShouldEqual("startDate");
    [Fact] void should_report_nothing() => _diagnostics.All.ShouldBeEmpty();
}
