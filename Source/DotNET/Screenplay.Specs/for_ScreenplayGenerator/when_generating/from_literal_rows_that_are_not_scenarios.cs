// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Screenplay.for_ScreenplayGenerator.given;

namespace Cratis.Arc.Screenplay.for_ScreenplayGenerator.when_generating;

public class from_literal_rows_that_are_not_scenarios : a_generated_document
{
    void Because() => Generate(LiteralCaseSources.With("""
        namespace Library.Authors.Registration;
        public abstract class publisher(string name)
        {
            public string Name => name;
        }
        public class North() : publisher("North");
        public class South() : publisher("South");
        """));

    [Fact] void should_not_diagnose_ordinary_inheritance_as_unfolded_cases() => Result.Diagnostics.Where(diagnostic => diagnostic.Code == ScreenplayDiagnosticCodes.SpecificationCasesNotFolded).ShouldBeEmpty();
    [Fact] void should_not_invent_a_specification() => Result.Source.ShouldNotContain("specification ");
    [Fact] void should_round_trip_and_bind() => AssertDocument();
}
