// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Screenplay.for_ScreenplayGenerator.given;

/// <summary>
/// Supplies real command wrappers with independent default and production routes.
/// </summary>
internal static class ProductionRouteSources
{
    internal const string Wrapper = "new EventForEventSourceId(OtherId, new AuthorRegistered(Name)) { EventSourceType = \"Ledger\", EventStreamType = \"Postings\", EventStreamId = Month }";

    internal static string With(string handler, string attributes = "EventSourceType(\"Account\"), EventStreamType(\"Main\")", string properties = "[Key] string Id, string OtherId, string Month, string Name") => $$"""
        using System;
        using System.Collections.Generic;
        using System.Threading.Tasks;
        using Cratis.Arc.Commands.ModelBound;
        using Cratis.Arc.Chronicle.Commands;
        using Cratis.Chronicle.Events;
        using Cratis.Chronicle.EventSequences;
        using Cratis.Chronicle.Keys;
        namespace Library.Authors.Registration;
        [EventType] public record AuthorRegistered(string Name);
        [EventType] public record AuthorAudited(string Name);
        [Cratis.Chronicle.EventSources.EventSource("stored-ledger")]
        [Cratis.Chronicle.EventSources.EventStream(LedgerEventSource.Postings)]
        public class LedgerEventSource : Cratis.Chronicle.EventSources.IEventSource
        {
            public const string Postings = "stored-postings";
        }
        [Command{{(attributes.Length == 0 ? string.Empty : ", " + attributes)}}]
        public record RegisterAuthor({{properties}})
        {
            {{handler}}
        }
        """;
}
