// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Screenplay.for_ScreenplayGenerator.given;

/// <summary>
/// Copies Stage's command that returns only an EventForEventSourceId, without an initializer.
/// </summary>
internal static class WrappedEventSources
{
    internal const string Command = """
        using Cratis.Arc.Commands.ModelBound;
        using Cratis.Chronicle.Events;
        using Cratis.Chronicle.EventSequences;
        namespace Library.Authors.Registration;
        [EventType] public record AuthorRegistered(string Name);
        [Command, EventSourceType("Account"), EventStreamType("Main")]
        public record RegisterAuthor(string OtherId, string Name)
        {
            public EventForEventSourceId Handle() => new EventForEventSourceId(OtherId, new AuthorRegistered(Name));
        }
        """;
}
