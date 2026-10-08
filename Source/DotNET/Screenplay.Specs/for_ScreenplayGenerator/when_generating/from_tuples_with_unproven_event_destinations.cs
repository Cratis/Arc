// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Screenplay.for_ScreenplayGenerator.given;

namespace Cratis.Arc.Screenplay.for_ScreenplayGenerator.when_generating;

public class from_tuples_with_unproven_event_destinations : a_generated_document
{
    [Theory]
    [InlineData("public (AuthorRegistered, AuthorId) Handle() => (new(Name), new AuthorId(Id));")]
    [InlineData("public (AuthorRegistered, object) Handle() => (new(Name), new AuthorId(Id));")]
    [InlineData("public (AuthorRegistered, object) Handle() => (new(Name), Name);")]
    [InlineData("public (AuthorRegistered, IComparable) Handle() => (new(Name), Name);")]
    [InlineData("public (AuthorRegistered, OneOf.OneOf<EventSourceId, string>) Handle() => (new AuthorRegistered(Name), new OneOf.OneOf<EventSourceId, string>());")]
    [InlineData("public (AuthorRegistered, OneOf.OneOf<string, OneOf.OneOf<EventSourceId, int>>) Handle() => (new(Name), new());")]
    [InlineData("public (AuthorRegistered, Cratis.Monads.Result<EventSourceId, string>) Handle() => (new(Name), new());")]
    [InlineData("public (AuthorRegistered, CustomUnion) Handle() => (new(Name), new());")]
    [InlineData("public (AuthorRegistered, RecursiveUnion) Handle() => (new(Name), new());")]
    [InlineData("public void Handle() { var unused = (new AuthorRegistered(Name), Name); }")]
    [InlineData("public async System.Threading.Tasks.Task Handle(Cratis.Chronicle.EventSequences.IEventLog log) { (AuthorRegistered, string) Construct() => (new(Name), Name); await log.Append(OtherId, Construct().Item1); }")]
    public void should_not_claim_the_command_identity_for_an_unproven_tuple(string handler)
    {
        var source = IdentifierSources.With("""
            [Command] public record RegisterAuthor([Key] Guid Id, Guid OtherId, string Name)
            {
            """ + handler + "}");
        Generate((Analyzed.SlicePath, source), ("Unions.cs", Unions));

        Result.Source.ShouldNotContain(" identifier");
        Result.Source.ShouldNotContain("for id");
        Result.Model.Slices.SelectMany(slice => slice.Commands).Single().Produces.Single().UsesCommandContext.ShouldBeFalse();
        Result.Diagnostics.Any(diagnostic => diagnostic.Code == ScreenplayDiagnosticCodes.UnrepresentableProductionDestination).ShouldBeTrue();
        AssertDocument();
    }

    [Theory]
    [InlineData("OneOf.OneOf<string, int>")]
    [InlineData("OneOf.OneOf<string, string>")]
    [InlineData("OneOf.OneOf<string, OneOf.OneOf<int, bool>>")]
    [InlineData("Cratis.Monads.Result<string, int>")]
    public void should_keep_the_command_identity_when_every_union_branch_is_proven_safe(string response)
    {
        var source = IdentifierSources.With($$"""
            [Command] public record RegisterAuthor([Key] Guid Id, string Name)
            {
                public (AuthorRegistered, {{response}}) Handle() => (new(Name), new());
            }
            """);
        Generate((Analyzed.SlicePath, source), ("Unions.cs", Unions));
        Result.Source.ShouldContain("for id");
        Result.Source.ShouldContain(" identifier");
        AssertDocument();
    }

    const string Unions = """
        namespace OneOf
        {
            public interface IOneOf { object Value { get; } }
            public struct OneOf<T0, T1> : IOneOf { public object Value => default(T0)!; }
            public class OneOfBase<T0, T1> : IOneOf { public object Value => default(T0)!; }
        }
        namespace Cratis.Monads
        {
            public struct Result<T0, T1> : OneOf.IOneOf { public object Value => default(T0)!; }
        }
        namespace Library.Authors.Registration
        {
            public class RecursiveUnion : OneOf.OneOfBase<RecursiveUnion, string> { }
            public class CustomUnion : OneOf.IOneOf
            {
                public object Value => new Cratis.Chronicle.Events.EventSourceId("other");
            }
        }
        """;
}
