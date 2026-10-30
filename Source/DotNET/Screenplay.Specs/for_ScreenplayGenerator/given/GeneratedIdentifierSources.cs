// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Screenplay.for_ScreenplayGenerator.given;

/// <summary>
/// Copies Stage's direct conversion and EventSourceId((value).ToString()) routing forms.
/// </summary>
internal static class GeneratedIdentifierSources
{
    internal static string With(string type, string identity, string? destination = null, bool block = false) => $$"""
        #nullable enable
        using System;
        using System.Globalization;
        using Cratis.Arc.Commands.ModelBound;
        using Cratis.Arc.Chronicle.Commands;
        using Cratis.Chronicle.Events;
        using Cratis.Chronicle.EventSequences;
        using Cratis.Concepts;
        namespace Library.Authors.Registration;
        public record TextIdentity(string Value) : ConceptAs<string>(Value);
        public record NumberIdentity(long Value) : ConceptAs<long>(Value);
        public record UuidIdentity(Guid Value) : ConceptAs<Guid>(Value);
        public record SourceIdentity(Guid Value) : EventSourceId<Guid>(Value);
        [EventType] public record AuthorRegistered(string Name);
        [Command, EventSourceType("Account"), EventStreamType("Main")]
        public record RegisterAuthor({{type}} Id, {{type}} OtherId, string Name) : ICanProvideEventSourceId
        {
            public EventSourceId GetEventSourceId() {{(block ? "{ return " + identity + "; }" : "=> " + identity + ";")}}
            public {{(destination is null ? "AuthorRegistered" : "EventForEventSourceId")}} Handle() =>
                {{(destination is null ? "new(Name)" : "new EventForEventSourceId(" + destination + ", new AuthorRegistered(Name))")}};
        }
        """;
}
