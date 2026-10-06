// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using ModelRuleKind = Cratis.Arc.Screenplay.Model.ValidationRuleKind;

namespace Cratis.Arc.Screenplay.for_ValidationSyntaxBuilder.when_building;

/// <summary>
/// A named rule cannot carry machine-local paths even when the model was supplied directly.
/// </summary>
public class a_named_rule_with_a_rooted_path : given.a_validation_syntax_builder
{
    [Theory]
    [InlineData("/external/Predicates.cs")]
    [InlineData("C:/external/Predicates.cs")]
    public void should_leave_the_rule_out_and_report_it(string path)
    {
        var blocks = _builder.Build(
            [new("Name", ModelRuleKind.Rule, "IsKnown", null) { SourceFilePath = path }],
            "Library.Authors.Registration");

        blocks.ShouldBeEmpty();
        _diagnostics.All.Select(diagnostic => diagnostic.Code).ShouldContainOnly([ScreenplayDiagnosticCodes.UnmappableValidationRule]);
    }
}
