// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Screenplay.for_ScreenplayGenerator.when_generating.with_authoring_only_constructs;

public class a_generated_factory : an_authoring_document
{
    void Because() => Generate("""
        using System;
        using Cratis.Arc.Commands.ModelBound;
        using Cratis.Chronicle.Events;
        namespace Library.Authors.Registration;
        public record AuthorId(Guid Value) : EventSourceId<Guid>(Value)
        {
            public static AuthorId New() => new(Guid.NewGuid());
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
        """);

    [Fact] void should_generate_the_factory_result() => Result.Source.ShouldContain("authorId AuthorId generated identifier");
    [Fact] void should_return_the_generated_value() => Result.Source.ShouldContain("returns authorId");
    [Fact] void should_recover_the_inferred_local_as_required() => Result.Model.Slices.SelectMany(slice => slice.Commands).Single().Authoring!.Generated.Single().Type.IsOptional.ShouldBeFalse();
    [Fact] void should_bind_both_modes_as_v7() => AssertExecutableDocument();
    [Fact] void should_generate_values_by_default() => Off.Source.ShouldContain("authorId AuthorId generated identifier");
}
