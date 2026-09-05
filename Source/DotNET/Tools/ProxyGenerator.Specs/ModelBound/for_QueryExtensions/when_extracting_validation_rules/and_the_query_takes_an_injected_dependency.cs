// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reflection;

namespace Cratis.Arc.ProxyGenerator.ModelBound.for_QueryExtensions.when_extracting_validation_rules;

/// <summary>
/// Almost every real query takes a collection or service alongside its arguments. Requiring the argument model to
/// mirror those too would find no model and emit no client rules, while the server — which only ever sees the
/// caller's arguments — validated fine. The two sides have to consider the same parameter set.
/// </summary>
public class and_the_query_takes_an_injected_dependency : Specification
{
    IEnumerable<Templates.QueryDescriptor> _result;

    void Because() => _result = typeof(ReadModelWithDependency).GetTypeInfo().ToQueryDescriptors(
        "/output",
        segmentsToSkip: 5,
        skipQueryNameInRoute: true,
        apiPrefix: "api",
        [typeof(ReadModelWithDependency).GetTypeInfo()]);

    [Fact] void should_emit_rules_for_the_argument() => _result.Single().ValidationRules.Select(_ => _.PropertyName).ShouldContain("term");
    [Fact] void should_emit_the_rule_from_the_argument_set_validator() => _result.Single().ValidationRules.Single(_ => _.PropertyName == "term").Rules.Select(_ => _.RuleName).ShouldContain("minLength");
    [Fact] void should_not_emit_rules_for_the_dependency() => _result.Single().ValidationRules.Select(_ => _.PropertyName).ShouldNotContain("dependency");
}
