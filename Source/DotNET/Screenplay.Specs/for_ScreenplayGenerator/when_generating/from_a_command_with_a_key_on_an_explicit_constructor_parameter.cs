// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Screenplay.for_ScreenplayGenerator.given;

namespace Cratis.Arc.Screenplay.for_ScreenplayGenerator.when_generating;

public class from_a_command_with_a_key_on_an_explicit_constructor_parameter : a_generated_document
{
    void Because() => Generate((Analyzed.SlicePath, IdentifierSources.With("""
        [Command]
        public class RegisterAuthor
        {
            public RegisterAuthor() { }
            private RegisterAuthor([Key] Guid Id) { this.Id = Id; }
            public Guid Id { get; set; }
            public string Name { get; set; } = "";
            public AuthorRegistered Handle() => new(Name);
        }
        """)));

    [Fact] void should_mark_the_identity() => Result.Source.ShouldContain("id Uuid identifier");
    [Fact] void should_name_the_production_destination() => Result.Source.ShouldContain("for id");
    [Fact] void should_report_no_generation_diagnostics() => Result.Diagnostics.ShouldBeEmpty();
    [Fact] void should_compile_round_trip_and_bind() => AssertDocument();
}
