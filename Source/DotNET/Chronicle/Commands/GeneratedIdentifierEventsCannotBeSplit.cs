// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Chronicle.Commands;

/// <summary>
/// The exception that is thrown when an immediate fallback cannot append a generated source atomically in returned event order.
/// </summary>
public class GeneratedIdentifierEventsCannotBeSplit() : Exception("Events for a generated source cannot be split across immediate appends. Return them contiguously, return EventsWithConcurrencyScopes, or use a command transaction.");
