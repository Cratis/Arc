// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Chronicle.Occurrences;

/// <summary>
/// The exception that is thrown when a command executed for an occurrence completes an event stream. Stream completion is applied outside the atomic commit and so cannot be covered by the effect receipt.
/// </summary>
/// <param name="message">The message describing what was refused.</param>
public class OccurrenceCommandCannotCompleteStreams(string message) : Exception(message);
