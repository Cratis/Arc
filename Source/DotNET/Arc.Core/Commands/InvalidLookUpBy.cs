// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Commands;

/// <summary>
/// The exception that is thrown when a lookup declaration names no command property or targets no read model.
/// </summary>
/// <param name="commandType">The command type.</param>
/// <param name="property">The declared command property.</param>
public class InvalidLookUpBy(Type commandType, string property) : Exception($"LookUpBy('{property}') on command '{commandType}' requires a read-model parameter and an existing command property.");
