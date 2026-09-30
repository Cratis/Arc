// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Chronicle.Testing.Commands;

/// <summary>
/// The exception that is thrown when events are seeded while the command is executing.
/// </summary>
public class EventsMustBeSeededBeforeExecution() : Exception("Seed events before executing the command.");
