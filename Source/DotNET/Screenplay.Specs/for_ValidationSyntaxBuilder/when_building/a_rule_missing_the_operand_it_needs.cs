// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Syntax;
using ModelRuleKind = Cratis.Arc.Screenplay.Model.ValidationRuleKind;

namespace Cratis.Arc.Screenplay.for_ValidationSyntaxBuilder.when_building;

/// <summary>
/// A comparison with nothing to compare against prints an operator with no operand, which does not parse. It is left
/// out and reported rather than emitted as something that breaks the document.
/// </summary>
public class a_rule_missing_the_operand_it_needs : given.a_validation_syntax_builder
{
    IEnumerable<ValidateSyntax> _blocks;

    void Because() => _blocks = _builder.Build(
        [new("Count", ModelRuleKind.GreaterThan, null, null)],
        "Library.Inventory.Adding");

    [Fact] void should_leave_the_rule_out() => _blocks.ShouldBeEmpty();
    [Fact] void should_report_the_rule() => _diagnostics.All.Select(_ => _.Code).ShouldContainOnly([ScreenplayDiagnosticCodes.ValidationRuleWithoutOperand]);
    [Fact] void should_locate_the_report_where_the_rule_was_declared() => _diagnostics.All.Single().Location.ShouldEqual("Library.Inventory.Adding");
}
