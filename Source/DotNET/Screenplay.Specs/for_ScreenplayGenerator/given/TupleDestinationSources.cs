// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Screenplay.for_ScreenplayGenerator.given;

/// <summary>
/// Supplies Stage's inline response and destination-wrapper tuples without synthetic route metadata.
/// </summary>
internal static class TupleDestinationSources
{
    internal const string Wrapper = "new EventForEventSourceId(new EventSourceId((OtherId).ToString()), new AuthorRegistered(Name))";
    internal const string RoutedWrapper = "new EventForEventSourceId(ThirdId, new AuthorAudited(Name)) { EventSource = typeof(LedgerEventSource), EventStream = LedgerEventSource.Postings, EventStreamId = Month }";

    internal static string With(string handler) => ProductionRouteSources.With(handler,
        properties: "[Key] string Id, string OtherId, string ThirdId, string Month, string Name") + """

        public record Receipt(string Name);
        public record ConvertedReceipt(string Name)
        {
            public static implicit operator Receipt(ConvertedReceipt value) => new(value.Name);
        }
        """;
}
