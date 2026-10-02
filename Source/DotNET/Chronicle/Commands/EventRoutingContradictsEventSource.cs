// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Chronicle.Commands;

/// <summary>
/// The exception that is thrown when a command's string routing attributes contradict its event source declaration.
/// </summary>
/// <param name="commandType">The command type with contradictory routing.</param>
/// <param name="dimension">The contradictory routing dimension.</param>
/// <param name="expected">The value declared by the event source.</param>
/// <param name="actual">The value declared by the command attribute.</param>
public class EventRoutingContradictsEventSource(Type commandType, string dimension, string expected, string actual)
    : Exception($"Command '{commandType.FullName ?? commandType.Name}' declares {dimension} '{actual}', which contradicts event source routing '{expected}'.");
