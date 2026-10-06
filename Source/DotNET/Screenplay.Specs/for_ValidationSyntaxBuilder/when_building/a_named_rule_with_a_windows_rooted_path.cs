// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Syntax;
using ModelRuleKind = Cratis.Arc.Screenplay.Model.ValidationRuleKind;

namespace Cratis.Arc.Screenplay.for_ValidationSyntaxBuilder.when_building;

public class a_named_rule_with_a_windows_rooted_path : given.a_validation_syntax_builder
{
    IEnumerable<ValidateSyntax> _blocks;

    void Because() => _blocks = _builder.Build(
        [new("Name", ModelRuleKind.Rule, "IsKnown", null) { SourceFilePath = "C:/external/Predicates.cs" }],
        "Library.Authors.Registration").ToList();

    [Fact] void should_leave_the_rule_out() => _blocks.ShouldBeEmpty();
    [Fact] void should_report_the_omission() => _diagnostics.All.Select(diagnostic => diagnostic.Code).ShouldContainOnly([ScreenplayDiagnosticCodes.UnmappableValidationRule]);
}
