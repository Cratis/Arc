// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Screenplay.for_ScreenplayGenerator.given;
using Cratis.Screenplay;
using Cratis.Screenplay.Syntax;

namespace Cratis.Arc.Screenplay.for_ScreenplayGenerator.when_generating;

public class from_an_inline_event_with_metadata : a_batch_a_document
{
    EventSyntax _event;

    void Because()
    {
        Generate((Analyzed.SlicePath, """
            using System;
            using Cratis.Arc.Commands.ModelBound;
            using Cratis.Chronicle.Events;
            using Cratis.Chronicle.Keys;
            namespace Library.Authors.Registration;
            /// <summary>An author acquired a name.</summary>
            /// <remarks>The **name** is the author's display name.</remarks>
            [EventType("AuthorNamed")]
            public record AuthorRegistered(string Name);
            [Command]
            public record RegisterAuthor([Key] Guid Id, string Name)
            {
                public AuthorRegistered Handle() => new(Name);
            }
            """));
        var application = new ScreenplayCompiler().Compile(Result.Source).Value!;
        _event = application.Modules.Single().Features.Single().Slices.Single().Commands.Single().Produces.Single().InlineEvent!;
    }

    [Fact] void should_declare_the_event_inline() => Result.Source.ShouldContain("produces event AuthorRegistered");
    [Fact] void should_round_trip_the_description() => _event.Description.ShouldEqual("An author acquired a name.");
    [Fact] void should_round_trip_the_documentation() => _event.Documentation.ShouldEqual("The **name** is the author's display name.");
    [Fact] void should_round_trip_the_pin() => _event.Id.ShouldEqual("AuthorNamed");
    [Fact] void should_compile_round_trip_and_bind() => AssertDocument();
}
