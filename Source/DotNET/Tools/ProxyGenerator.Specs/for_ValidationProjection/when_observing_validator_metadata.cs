// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.ProxyGenerator.for_ValidationProjection;

public class when_observing_validator_metadata : Specification
{
    ValidationExtraction _result;
    int _constructions;

    void Because()
    {
        var before = ProjectionCommandValidator.Constructions;
        _result = ValidationRulesExtractor.ExtractWithProjection(typeof(ProjectionCommand).Assembly, typeof(ProjectionCommand));
        _constructions = ProjectionCommandValidator.Constructions - before;
    }

    [Fact] void should_construct_the_validator_only_once() => _constructions.ShouldEqual(1);
    [Fact] void should_not_invoke_any_predicate_message_or_severity_delegate() => _result.ShouldNotBeNull();
    [Fact] void should_explain_custom_async_and_conditional_rules() => new[] { "unsupported-validator", "async", "conditional" }.All(reason => _result.Projection.Any(_ => _.Reason == reason)).ShouldBeTrue();
    [Fact] void should_explain_dynamic_metadata() => new[] { "dynamic-severity", "deferred-message" }.All(reason => _result.Projection.Any(_ => _.Reason == reason)).ShouldBeTrue();
    [Fact] void should_report_the_overridden_annotation() => _result.Projection.Any(_ => _.Member == "text" && _.Reason == "overridden-annotation").ShouldBeTrue();
}
