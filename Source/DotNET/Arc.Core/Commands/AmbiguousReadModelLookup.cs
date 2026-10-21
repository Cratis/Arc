// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Commands;

/// <summary>
/// The exception that is thrown when several command properties match a read model's key type.
/// </summary>
/// <param name="commandType">The command type.</param>
/// <param name="readModelType">The read model type.</param>
/// <param name="properties">The matching property names.</param>
public class AmbiguousReadModelLookup(Type commandType, Type readModelType, IEnumerable<string> properties)
    : Exception($"Command '{commandType}' has several lookup properties for '{readModelType}': {string.Join(", ", properties)}. Select one with [LookUpBy(nameof(property))].");
