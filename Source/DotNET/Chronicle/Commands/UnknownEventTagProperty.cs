// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Chronicle.Commands;

/// <summary>
/// The exception that is thrown when an event tag names a missing or unreadable command property.
/// </summary>
/// <param name="commandType">The command type.</param>
/// <param name="property">The property name.</param>
public class UnknownEventTagProperty(Type commandType, string property)
    : Exception($"Event tag property '{property}' on command '{commandType.FullName}' must be a readable public instance property without index parameters.");
