// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Chronicle.Testing.Commands;

/// <summary>
/// The exception that is thrown when competing events are queued while the command is executing.
/// </summary>
public class CompetingEventsMustBeQueuedBeforeExecution() : Exception("Queue competing events before executing the command.");
