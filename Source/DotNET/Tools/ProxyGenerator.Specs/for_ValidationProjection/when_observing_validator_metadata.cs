// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.ProxyGenerator.for_ValidationProjection;

public class when_observing_validator_metadata : Specification
{
    ValidationProjectionRecord[] _records;
    int _constructions;

    void Because()
    {
        var before = ProjectionCommandValidator.Constructions;
        _records = ProjectionExtraction.For(typeof(ProjectionCommand)).Records;
        _constructions = ProjectionCommandValidator.Constructions - before;
    }

    [Fact] void should_construct_the_validator_only_once() => _constructions.ShouldEqual(1);
    [Fact] void should_not_invoke_any_predicate_message_or_severity_delegate() => _records.ShouldNotBeNull();
    [Fact] void should_explain_unsupported_and_distinct_conditional_rules() => new[] { "unsupported-validator", "server-only-chained-condition", "server-only-block-condition", "server-only-async-condition" }.All(reason => _records.Any(_ => _.Reason == reason)).ShouldBeTrue();
    [Fact] void should_report_the_overridden_annotation() => _records.Any(_ => _.Member == "text" && _.Reason == "overridden-annotation").ShouldBeTrue();
}
