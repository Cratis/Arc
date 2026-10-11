// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Testing.Commands;

/// <summary>
/// The exception that is thrown when a strict scenario has no pin for a generated parameter.
/// </summary>
/// <param name="commandType">The command requiring the value.</param>
/// <param name="name">The generated parameter name.</param>
public class GeneratedValueNotPinned(Type commandType, string name) : Exception(
    $"Generated value '{name}' for command '{commandType.FullName}' is not pinned. " +
    $"Call PinGeneratedValue(\"{name}\", …) (or PinGeneratedValue<TCommand, {commandType.Name}>(\"{name}\", …) for a nested command) " +
    "before execution, or call AllowGeneration().");
