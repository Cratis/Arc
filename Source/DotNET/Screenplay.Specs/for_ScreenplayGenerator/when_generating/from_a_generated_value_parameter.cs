// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Screenplay.for_ScreenplayGenerator.given;

namespace Cratis.Arc.Screenplay.for_ScreenplayGenerator.when_generating;

public class from_a_generated_value_parameter : a_generated_document
{
    void Because() => Generate((Analyzed.SlicePath, GeneratedValueSources.With("""
        [Command] public record RegisterAuthor(string Name)
        {
            public AuthorRegistered Handle([GeneratedValue("receipt")] AuthorId value) => new(Name, value);
        }
        """).Replace("public record AuthorRegistered(string Name);", "public record AuthorRegistered(string Name, AuthorId Receipt);", StringComparison.Ordinal)));

    [Fact] void should_recover_the_explicit_pin_name() => Result.Source.ShouldContain("receipt AuthorId generated");
    [Fact] void should_map_the_parameter_by_its_explicit_name() => Result.Source.ShouldContain("receipt = receipt");
    [Fact] void should_not_infer_an_identifier_from_a_value_in_the_payload() => Result.Source.ShouldNotContain("generated identifier");
    [Fact] void should_bind_and_round_trip() => AssertDocument();
}
