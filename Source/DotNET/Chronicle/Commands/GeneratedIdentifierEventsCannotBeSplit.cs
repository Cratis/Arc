// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Chronicle.Commands;

/// <summary>
/// The exception that was thrown when an immediate fallback could not append a generated source atomically in returned event order. No longer thrown.
/// </summary>
[Obsolete("No longer thrown: events for several routes of one event source append as one batch.")]
public class GeneratedIdentifierEventsCannotBeSplit() : Exception("Events for a generated source cannot be split across immediate appends. Return them contiguously, return EventsWithConcurrencyScopes, or use a command transaction.");
