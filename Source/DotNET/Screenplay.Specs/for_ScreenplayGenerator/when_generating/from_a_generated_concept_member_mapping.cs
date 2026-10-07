// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Screenplay.for_ScreenplayGenerator.given;

namespace Cratis.Arc.Screenplay.for_ScreenplayGenerator.when_generating;

public class from_a_generated_concept_member_mapping : a_generated_document
{
    void Because() => Generate((Analyzed.SlicePath, """
        using System;
        using Cratis.Arc.Commands.ModelBound;
        using Cratis.Chronicle.Events;
        using Cratis.Chronicle.Keys;
        namespace Library.Authors.Registration;
        public record AuthorId(Guid Value) : EventSourceId<Guid>(Value);
        [EventType] public record AuthorRegistered(string Name, Guid? Copy);
        [Command] public record RegisterAuthor(string Name)
        {
            public (AuthorId, AuthorRegistered) Handle()
            {
                AuthorId authorId = new(Guid.NewGuid());
                return (authorId, new(Name, authorId.Value));
            }
        }
        """));

    [Fact] void should_keep_the_generated_identifier() => Result.Source.ShouldContain("authorId AuthorId generated identifier");
    [Fact] void should_keep_the_optional_event_property() => Result.Source.ShouldContain("copy Uuid optional");
    [Fact] void should_omit_the_member_mapping() => Result.Source.ShouldNotContain("copy = authorId");
    [Fact] void should_keep_the_command_property_mapping() => Result.Source.ShouldContain("name = name");
    [Fact] void should_report_the_omitted_production_mapping() => Result.Diagnostics.Single(diagnostic => diagnostic.Code == ScreenplayDiagnosticCodes.UnmappableCommandProduction).Message.ShouldContain("AuthorRegistered.Copy");
    [Fact] void should_compile_round_trip_and_bind() => AssertDocument();
}
