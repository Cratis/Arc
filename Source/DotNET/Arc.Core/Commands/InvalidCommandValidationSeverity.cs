// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Commands;

/// <summary>
/// The exception that is thrown when a command declares an unsupported blocking validation severity.
/// </summary>
/// <param name="commandType">The command with the unsupported severity.</param>
public sealed class InvalidCommandValidationSeverity(Type commandType) : Exception($"Command '{commandType}' declares an unsupported blocking validation severity.");
