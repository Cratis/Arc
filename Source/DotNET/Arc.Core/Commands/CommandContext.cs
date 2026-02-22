// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Validation;
using Cratis.Execution;

namespace Cratis.Arc.Commands;

/// <summary>
/// Represents the context for a command being executed.
/// </summary>
/// <param name="CorrelationId">The correlation ID for the command.</param>
/// <param name="Type">The type of the command.</param>
/// <param name="Command">The command instance.</param>
/// <param name="Dependencies">The dependencies required to handle the command.</param>
/// <param name="Values">A set of values associated with the command context.</param>
/// <param name="AllowedSeverity">The maximum validation result severity level to allow. Validation results with severity higher than this will cause the command to fail.</param>
/// <param name="Response">The optional response from handling the command, if any.</param>
public record CommandContext(
    CorrelationId CorrelationId,
    Type Type,
    object Command,
    IEnumerable<object> Dependencies,
    CommandContextValues Values,
    ValidationResultSeverity? AllowedSeverity = default,
    object? Response = default);
