// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Chronicle.Commands;

/// <summary>
/// The exception that is thrown when an event tag does not specify exactly one property or constant value.
/// </summary>
/// <param name="commandType">The command type.</param>
/// <param name="name">The tag name.</param>
public class AmbiguousEventTagValue(Type commandType, string name)
    : Exception($"Event tag '{name}' on command '{commandType.FullName}' must specify exactly one property or constant Value.");
