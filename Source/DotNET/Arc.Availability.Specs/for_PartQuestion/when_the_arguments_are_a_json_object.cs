// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Availability.for_PartQuestion;

public class when_the_arguments_are_a_json_object : Specification
{
    PartQuestion _question;

    void Because() => _question = PartQuestion.From("Some.Part", "{\"vesselId\":\"v1\",\"count\":3,\"nested\":{\"Id\":\"n\"}}");

    [Fact] void should_read_a_string() => _question.Arguments["VESSELID"].ShouldEqual("v1");
    [Fact] void should_read_an_integer_as_long() => _question.Arguments["count"].ShouldEqual(3L);
    [Fact] void should_read_nested_objects_ignoring_case() => ((IReadOnlyDictionary<string, object>)_question.Arguments["nested"])["id"].ShouldEqual("n");
}
