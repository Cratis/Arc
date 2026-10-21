// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Syntax;
using ModelRuleKind = Cratis.Arc.Screenplay.Model.ValidationRuleKind;
using SyntaxRuleKind = Cratis.Screenplay.Syntax.ValidationRuleKind;

namespace Cratis.Arc.Screenplay.for_ValidationSyntaxBuilder.when_building;

public class date_literal_rules : given.a_validation_syntax_builder
{
    [Theory]
    [InlineData(ModelRuleKind.GreaterThan, SyntaxRuleKind.GreaterThan)]
    [InlineData(ModelRuleKind.GreaterThanOrEqual, SyntaxRuleKind.GreaterThanOrEqual)]
    [InlineData(ModelRuleKind.LessThan, SyntaxRuleKind.LessThan)]
    [InlineData(ModelRuleKind.LessThanOrEqual, SyntaxRuleKind.LessThanOrEqual)]
    [InlineData(ModelRuleKind.Equal, SyntaxRuleKind.Equal)]
    [InlineData(ModelRuleKind.NotEqual, SyntaxRuleKind.NotEqual)]
    public void should_preserve_the_operator_and_canonical_text(ModelRuleKind modelKind, SyntaxRuleKind syntaxKind)
    {
        const string Instant = "2026-01-01T00:00:00.1234567Z";
        var rule = _builder.Build([new("End", modelKind, Instant, null)], "ReserveBook")
            .OfType<DeclarativeValidateSyntax>().Single().Rules.Single();
        rule.Rule.ShouldEqual(syntaxKind);
        ((LiteralExpressionSyntax)rule.Value!).Value.ShouldEqual(Instant);
        _diagnostics.All.ShouldBeEmpty();
    }
}
