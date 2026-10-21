// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Chronicle.Occurrences;

/// <summary>
/// The exception that is thrown when a command executed for an occurrence commits its unit of work early, for example through an aggregate root commit. The effect receipt is only written with the final commit of the unit of work.
/// </summary>
/// <param name="message">The message describing what was refused.</param>
public class OccurrenceCommandCannotCommitEarly(string message) : Exception(message);
