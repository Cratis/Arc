// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Screenplay.for_ScreenplayGenerator.given;

namespace Cratis.Arc.Screenplay.for_ScreenplayGenerator.when_generating;

/// <summary>
/// An existing payload copy of the identity remains intact and is reported by the language as information.
/// </summary>
public class from_a_command_copying_its_identifier_into_the_event : a_batch_a_document
{
    void Because() => Generate((Analyzed.SlicePath, """
        using System;
        using Cratis.Arc.Commands.ModelBound;
        using Cratis.Chronicle.Events;
        using Cratis.Chronicle.Keys;

        namespace Library.Authors.Registration;

        [EventType]
        public record AuthorRegistered(Guid Id, string Name);

        [Command]
        public record RegisterAuthor([Key] Guid Id, string Name)
        {
            public AuthorRegistered Handle() => new(Id, Name);
        }
        """));

    [Fact] void should_keep_the_persisted_contract_standalone() => Result.Source.ShouldNotContain("produces event AuthorRegistered");
    [Fact] void should_state_the_destination() => Result.Source.ShouldContain("for id");
    [Fact] void should_preserve_the_payload_copy() => Result.Source.ShouldContain("id = id");
    [Fact] void should_report_the_payload_copy_as_information() => RoundTrip.Diagnostics.Any(diagnostic => diagnostic.Code == "PLAY0469" && diagnostic.Severity == Cratis.Screenplay.Diagnostics.DiagnosticSeverity.Information).ShouldBeTrue();
    [Fact] void should_compile_round_trip_and_bind() => AssertDocument();
}
