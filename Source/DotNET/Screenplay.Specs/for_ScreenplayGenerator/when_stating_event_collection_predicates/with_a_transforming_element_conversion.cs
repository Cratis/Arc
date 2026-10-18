// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Screenplay.for_ScreenplayGenerator.when_stating_event_collection_predicates;

public class with_a_transforming_element_conversion : given.an_event_collection_predicate
{
    [Theory]
    [InlineData("new(value.ToUpperInvariant())", "Value")]
    [InlineData("new(value)", "Value.ToUpperInvariant()")]
    public void should_omit_the_scenario_instead_of_stating_the_unconverted_constant(string conversion, string backing)
    {
        GenerateExpectation("e.Tags.SequenceEqual<ApprovalLabel>([Urgent, \"review\"])", fixture: "[]", conversion: conversion, backing: backing);
        AssertUnread();
        Result.Diagnostics.ShouldContain(diagnostic => diagnostic.Code == ScreenplayDiagnosticCodes.UnreadableSpecification &&
            diagnostic.Message.Contains("conversion", StringComparison.Ordinal) &&
            diagnostic.Message.Contains("unchanged", StringComparison.Ordinal));
    }
}
