// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Testing.Commands;

/// <summary>
/// The exception that is thrown when a pinned value is incompatible with a generated parameter's type.
/// </summary>
/// <param name="commandType">The command requiring the value.</param>
/// <param name="name">The generated parameter name.</param>
/// <param name="expectedType">The declared parameter type.</param>
/// <param name="actualType">The pinned type, or null for a null pin.</param>
public class PinnedGeneratedValueHasWrongType(Type commandType, string name, Type expectedType, Type? actualType) : Exception(
    $"Pinned generated value '{name}' for command '{commandType.FullName}' must have type '{expectedType.FullName}', " +
    $"but its type is '{actualType?.FullName ?? "null"}'. Pin a non-null value of the declared type.");
