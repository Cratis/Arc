// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Screenplay.for_ScreenplayGenerator.given;

namespace Cratis.Arc.Screenplay.for_ScreenplayGenerator.when_generating;

/// <summary>
/// A typed event source identity supplies every conditional and unconditional production destination.
/// </summary>
public class from_a_command_with_a_typed_identifier : a_generated_document
{
    void Because() => Generate((Analyzed.SlicePath, IdentifierSources.With("""
        [Command]
        public record RegisterAuthor(AuthorId Id, string Name, bool Active)
        {
            public AuthorRegistered Handle()
            {
                if (Active) return new(Name);
                return new("Inactive");
            }
        }
        """)));

    [Fact] void should_mark_the_identity() => Result.Source.ShouldContain("id AuthorId identifier");
    [Fact] void should_name_the_destination_of_each_production() => Result.Source.Split("for id", StringSplitOptions.None).Length.ShouldEqual(3);
    [Fact] void should_keep_the_identity_out_of_the_payload() => Result.Source.ShouldNotContain("id = id");
    [Fact] void should_report_no_generation_diagnostics() => Result.Diagnostics.ShouldBeEmpty();
    [Fact] void should_compile_round_trip_and_bind() => AssertDocument();
}
