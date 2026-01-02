// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.ProxyGenerator.Scenarios.Infrastructure;

/// <summary>
/// Represents the result of executing a command through the JavaScript proxy.
/// </summary>
/// <typeparam name="TResult">The type of the response data.</typeparam>
/// <param name="Result">The command result.</param>
/// <param name="RawJson">The raw JSON response.</param>
public record CommandExecutionResult<TResult>(Commands.CommandResult<TResult>? Result, string RawJson);
