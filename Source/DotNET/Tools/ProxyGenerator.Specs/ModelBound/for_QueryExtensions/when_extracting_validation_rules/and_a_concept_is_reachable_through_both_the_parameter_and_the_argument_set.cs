// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reflection;

namespace Cratis.Arc.ProxyGenerator.ModelBound.for_QueryExtensions.when_extracting_validation_rules;

/// <summary>
/// A concept's rules can be reached twice — once through the query's own parameter and once through the property of
/// the same type on the argument model. Emitting both would show the user the identical message twice.
/// </summary>
public class and_a_concept_is_reachable_through_both_the_parameter_and_the_argument_set : Specification
{
    IEnumerable<Templates.QueryDescriptor> _result;

    void Because() => _result = typeof(ReadModelWithConceptAndParameters).GetTypeInfo().ToQueryDescriptors(
        "/output",
        segmentsToSkip: 5,
        skipQueryNameInRoute: true,
        apiPrefix: "api",
        [typeof(ReadModelWithConceptAndParameters).GetTypeInfo()]);

    [Fact] void should_emit_rules_for_the_argument() => _result.Single().ValidationRules.Select(_ => _.PropertyName).ShouldContain("email");
    [Fact] void should_emit_the_concept_rule_once() => _result.Single().ValidationRules.Single(_ => _.PropertyName == "email").Rules.Count(_ => _.RuleName == "emailAddress").ShouldEqual(1);
}
