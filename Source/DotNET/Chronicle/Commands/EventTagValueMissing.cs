// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Chronicle.Commands;

/// <summary>
/// The exception that is thrown when an event tag property has a null value or renders as null.
/// </summary>
/// <param name="commandType">The command type.</param>
/// <param name="property">The property name.</param>
public class EventTagValueMissing(Type commandType, string property)
    : Exception($"Event tag property '{property}' on command '{commandType.FullName}' must have a non-null value.");
