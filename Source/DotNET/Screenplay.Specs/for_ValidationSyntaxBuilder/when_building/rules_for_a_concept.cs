// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Syntax;
using ModelRuleKind = Cratis.Arc.Screenplay.Model.ValidationRuleKind;

namespace Cratis.Arc.Screenplay.for_ValidationSyntaxBuilder.when_building;

/// <summary>
/// Rules inside a concept declaration carry no property name - the concept's own value is the implied subject - so
/// whatever member the rule was declared on is irrelevant to the document.
/// </summary>
public class rules_for_a_concept : given.a_validation_syntax_builder
{
    IEnumerable<ValidationRuleSyntax> _rules;

    void Because() => _rules = _builder
        .Build(
            [
                new("Value", ModelRuleKind.NotEmpty, null, null),
                new("SomethingElse", ModelRuleKind.Max, 200, null)
            ],
            "AuthorName",
            impliedSubject: true)
        .OfType<DeclarativeValidateSyntax>()
        .SelectMany(_ => _.Rules);

    [Fact] void should_leave_the_subject_of_every_rule_implied() => _rules.Select(_ => _.Property).Distinct().ShouldContainOnly([ValidationRuleSyntax.ConceptValue]);
    [Fact] void should_keep_every_rule() => _rules.Count().ShouldEqual(2);
}
