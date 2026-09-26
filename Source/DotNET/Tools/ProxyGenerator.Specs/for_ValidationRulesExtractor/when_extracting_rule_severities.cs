// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.ProxyGenerator.Templates;

namespace Cratis.Arc.ProxyGenerator.for_ValidationRulesExtractor;

public class when_extracting_rule_severities : Specification
{
    IEnumerable<PropertyValidationDescriptor> _result;

    void Because() => _result = ValidationRulesExtractor.ExtractValidationRules(typeof(TestCommandWithSeverities).Assembly, typeof(TestCommandWithSeverities));

    [Fact] void should_carry_warning() => _result.Single(_ => _.PropertyName == "warning").Rules.Single().Severity.ShouldEqual(2);
    [Fact] void should_carry_information() => _result.Single(_ => _.PropertyName == "information").Rules.Single().Severity.ShouldEqual(1);
    [Fact] void should_carry_default_error() => _result.Single(_ => _.PropertyName == "error").Rules.Single().Severity.ShouldEqual(3);
    [Fact] void should_leave_dynamic_severity_unknown() => _result.Single(_ => _.PropertyName == "dynamic").Rules.Single().Severity.ShouldBeNull();
}
