// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Chronicle.Testing.EventSequences;

/// <summary>
/// The exception that is thrown when an assertion on an event log or event sequence fails.
/// </summary>
/// <param name="message">The assertion failure message.</param>
public class EventLogAssertionException(string message) : Exception(message);
