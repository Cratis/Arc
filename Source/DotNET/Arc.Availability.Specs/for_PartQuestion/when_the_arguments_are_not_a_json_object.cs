// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Availability.for_PartQuestion;

public class when_the_arguments_are_not_a_json_object : Specification
{
    Exception _error;

    void Because() => _error = Catch.Exception(() => PartQuestion.From("Some.Part", "[1, 2]"));

    [Fact] void should_refuse_them() => _error.ShouldBeOfExactType<ArgumentException>();
}
