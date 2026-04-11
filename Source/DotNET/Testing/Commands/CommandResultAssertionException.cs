// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Testing.Commands;

/// <summary>
/// The exception that is thrown when an assertion on a <see cref="Cratis.Arc.Commands.CommandResult"/> fails.
/// </summary>
/// <param name="message">The assertion failure message.</param>
public class CommandResultAssertionException(string message) : Exception(message);
