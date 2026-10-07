// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Screenplay.for_ScreenplayGenerator.given;

namespace Cratis.Arc.Screenplay.for_ScreenplayGenerator.when_generating;

public class from_a_command_with_a_key_parameter_of_a_different_type : a_generated_document
{
    void Because() => Generate((Analyzed.SlicePath, IdentifierSources.With("""
        [Command]
        public class RegisterAuthor([Key] Guid Id, string Name)
        {
            public string Id { get; set; } = Id.ToString();
            public string Name { get; set; } = Name;
            public AuthorRegistered Handle() => new(Name);
        }
        """)));

    [Fact] void should_not_state_an_identifier() => Result.Source.ShouldNotContain("identifier");
    [Fact] void should_compile_round_trip_and_bind() => AssertDocument();
}
