// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Chronicle.Commands;

/// <summary>
/// The exception that is thrown when a stream id template references an unknown or unreadable command property.
/// </summary>
/// <param name="commandType">The command type.</param>
/// <param name="property">The referenced property.</param>
public class UnknownEventStreamIdTemplateProperty(Type commandType, string property)
    : Exception($"Event stream id template property '{property}' must be a readable public instance property on '{commandType.FullName}'.");
