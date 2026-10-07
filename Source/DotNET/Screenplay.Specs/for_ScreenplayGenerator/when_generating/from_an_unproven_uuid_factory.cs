// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Screenplay.for_ScreenplayGenerator.given;

namespace Cratis.Arc.Screenplay.for_ScreenplayGenerator.when_generating;

public class from_an_unproven_uuid_factory : a_generated_document
{
    [Theory]
    [InlineData("new(Guid.Empty)")]
    [InlineData("new(new Guid(\"fc065010-5e68-4051-a85d-4122f786de22\"))")]
    public void should_not_describe_a_fixed_uuid_as_generated(string creation)
    {
        Generate((Analyzed.SlicePath, $$"""
            using System;
            using Cratis.Arc.Commands.ModelBound;
            using Cratis.Chronicle.Events;
            namespace Library.Authors.Registration;
            public record AuthorId(Guid Value) : EventSourceId<Guid>(Value)
            {
                public static AuthorId New() => {{creation}};
            }
            [EventType] public record AuthorRegistered(string Name);
            [Command] public record RegisterAuthor(string Name)
            {
                public (AuthorId, AuthorRegistered) Handle()
                {
                    var authorId = AuthorId.New();
                    return (authorId, new(Name));
                }
            }
            """));

        Result.Source.ShouldNotContain("generated");
        Result.Source.ShouldNotContain("returns");
        Result.Diagnostics.Any(diagnostic => diagnostic.Code == ScreenplayDiagnosticCodes.UnreadableCommandResponse).ShouldBeTrue();
        AssertDocument();
    }

    [Theory]
    [InlineData("=> new(Guid.NewGuid());")]
    [InlineData("{ return new AuthorId(Guid.NewGuid()); }")]
    public void should_admit_a_proven_factory(string body)
    {
        Generate((Analyzed.SlicePath, $$"""
            using System;
            using Cratis.Arc.Commands.ModelBound;
            using Cratis.Chronicle.Events;
            namespace Library.Authors.Registration;
            public record AuthorId(Guid Value) : EventSourceId<Guid>(Value)
            {
                public static AuthorId New() {{body}}
            }
            [Command] public record RegisterAuthor()
            {
                public AuthorId Handle() => AuthorId.New();
            }
            """));

        Result.Source.ShouldContain("generated");
        Result.Source.ShouldContain("returns authorId");
        AssertDocument();
    }
}
