// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Chronicle.Occurrences;

/// <summary>
/// The exception that is thrown when a command executed for an occurrence appended events outside its unit of work, for example
/// through a direct append to an event sequence. Those events are committed without the effect receipt, so no receipt is
/// written and redelivering the occurrence would append them again.
/// </summary>
/// <param name="effectKey">The key of the effect.</param>
public class OccurrenceCommandAppendedImmediately(string effectKey)
    : Exception($"The command for effect '{effectKey}' appended events immediately instead of through its transactional unit of work. Those events are committed without an effect receipt. Return the events from the handler or append through the command's unit of work.");
