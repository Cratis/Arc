// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.ProxyGenerator;

/// <summary>
/// The exception that is thrown when a command exposes an operation collection instead of the explicit server-only batch.
/// </summary>
/// <param name="type">The unsupported return shape.</param>
public class UnsupportedCommandOperationCollection(Type type) : Exception($"Command return type '{type}' contains an operation collection. Use Cratis.Arc.Commands.CommandOperations instead; operation descriptors cannot be exposed to clients.");
