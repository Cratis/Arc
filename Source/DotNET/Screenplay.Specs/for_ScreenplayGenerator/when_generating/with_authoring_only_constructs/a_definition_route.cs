// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Screenplay.for_ScreenplayGenerator.given;

namespace Cratis.Arc.Screenplay.for_ScreenplayGenerator.when_generating.with_authoring_only_constructs;

public class a_definition_route : an_authoring_document
{
    void Because() => Generate(IdentifierSources.With("""
        [Cratis.Chronicle.EventSources.EventSource]
        [Cratis.Chronicle.EventSources.EventStream("Registration")]
        public class AuthorEventSource : Cratis.Chronicle.EventSources.IEventSource;
        [Command]
        [Cratis.Arc.Chronicle.Commands.EventSource<AuthorEventSource>("Registration")]
        public record RegisterAuthor(AuthorId AuthorId, string Name)
        {
            public AuthorRegistered Handle() => new(Name);
        }
        """));

    [Fact] void should_declare_the_definition_name() => Result.Source.ShouldContain("eventsource Author");
    [Fact] void should_route_to_the_declared_stream() => Result.Source.ShouldContain("stream Author.Registration");
    [Fact] void should_bind_the_authoring_route() => Bound.Success.ShouldBeTrue();
    [Fact] void should_not_report_admitted_routing_as_omitted() => Off.Diagnostics.Any(diagnostic => diagnostic.Code == ScreenplayDiagnosticCodes.EventSourceNotRepresentable).ShouldBeFalse();
    [Fact] void should_emit_definition_routes_by_default() => Off.Source.ShouldContain("eventsource Author");
}
