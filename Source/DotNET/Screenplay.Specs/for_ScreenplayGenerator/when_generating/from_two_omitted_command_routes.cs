// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Screenplay.Emission;
using Cratis.Arc.Screenplay.for_ScreenplayGenerator.given;
using Cratis.Arc.Screenplay.Model;

namespace Cratis.Arc.Screenplay.for_ScreenplayGenerator.when_generating;

public class from_two_omitted_command_routes : a_generated_document
{
    [Fact]
    public void should_identify_each_legacy_route()
    {
        Generate((Analyzed.SlicePath, IdentifierSources.With("""
            [Command, EventSourceType("Authors")]
            public record RegisterAuthor(AuthorId Id, string Name)
            {
                public AuthorRegistered Handle() => new(Name);
            }
            [Command, EventSourceType("Authors")]
            public record RenameAuthor(AuthorId Id, string Name)
            {
                public AuthorRegistered Handle() => new(Name);
            }
            """)));
        AssertRoutes(Result.Diagnostics, ScreenplayDiagnosticCodes.EventSourceNotRepresentable);
        AssertDocument();
    }

    [Fact]
    public void should_state_each_event_source_definition_route()
    {
        Generate((Analyzed.SlicePath, IdentifierSources.With("""
            [Cratis.Chronicle.EventSources.EventSource]
            [Cratis.Chronicle.EventSources.EventStream("Registration")]
            public class AuthorEventSource : Cratis.Chronicle.EventSources.IEventSource;
            [Command, Cratis.Arc.Chronicle.Commands.EventSource<AuthorEventSource>("Registration")]
            public record RegisterAuthor(AuthorId Id, string Name)
            {
                public AuthorRegistered Handle() => new(Name);
            }
            [Command, Cratis.Arc.Chronicle.Commands.EventSource<AuthorEventSource>("Registration")]
            public record RenameAuthor(AuthorId Id, string Name)
            {
                public AuthorRegistered Handle() => new(Name);
            }
            """)));
        Result.Source.Split("stream Author.Registration", StringSplitOptions.None).Length.ShouldEqual(3);
        Result.Diagnostics.Any(diagnostic => diagnostic.Code == ScreenplayDiagnosticCodes.EventSourceNotRepresentable).ShouldBeFalse();
        AssertDocument();
    }

    [Fact]
    public void should_identify_each_unproven_returned_destination()
    {
        Generate((Analyzed.SlicePath, IdentifierSources.With("""
            [Command] public record RegisterAuthor(AuthorId Id, string Name)
            {
                public (AuthorId, AuthorRegistered) Handle() => (Id, new AuthorRegistered(Name));
            }
            [Command] public record RenameAuthor(AuthorId Id, string Name)
            {
                public (AuthorId, AuthorRegistered) Handle() => (Id, new AuthorRegistered(Name));
            }
            """)));
        AssertRoutes(Result.Diagnostics, ScreenplayDiagnosticCodes.UnmappableEventSourceIdResult);
        AssertDocument();
    }

    [Fact]
    public void should_identify_each_unadmitted_generated_destination()
    {
        Generate((Analyzed.SlicePath, IdentifierSources.With("""
            [Command] public record RegisterAuthor(string Name)
            {
                public (AuthorId, AuthorRegistered) Handle()
                {
                    var authorId = new AuthorId(Guid.NewGuid());
                    return (authorId, new AuthorRegistered(Name));
                }
            }
            [Command] public record RenameAuthor(string Name)
            {
                public (AuthorId, AuthorRegistered) Handle()
                {
                    var authorId = new AuthorId(Guid.NewGuid());
                    return (authorId, new AuthorRegistered(Name));
                }
            }
            """)));
        var model = Result.Model with
        {
            Slices = Result.Model.Slices.Select(slice => slice with
            {
                Commands = slice.Commands.Select(command => command with
                {
                    Authoring = command.Authoring! with { Generated = [new("authorId", new TypeReferenceModel("Uuid", false, false))] }
                }).ToList()
            }).ToList()
        };
        AssertRoutes(new ScreenplayEmitter().Emit(model, new()).Diagnostics, ScreenplayDiagnosticCodes.UnmappableEventSourceIdResult);
        AssertDocument();
    }

    static void AssertRoutes(IEnumerable<ScreenplayDiagnostic> diagnostics, string code)
    {
        var routes = diagnostics.Where(diagnostic => diagnostic.Code == code).Distinct().ToArray();
        routes.Length.ShouldEqual(2);
        foreach (var command in new[] { "RegisterAuthor", "RenameAuthor" })
        {
            routes.Single(diagnostic => diagnostic.Location == $"Library.Authors.Registration.{command}").Message.ShouldContain($"command '{command}'");
        }
    }
}
