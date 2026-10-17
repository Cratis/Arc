// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Chronicle.Occurrences;

/// <summary>
/// The exception that is thrown when an occurrence command is executed from within a running command or another occurrence command. One effect receipt covers one unit of work, so the executor must begin the unit of work itself.
/// </summary>
/// <param name="message">The message describing what was refused.</param>
public class OccurrenceCommandCannotRunInsideCommand(string message) : Exception(message);
