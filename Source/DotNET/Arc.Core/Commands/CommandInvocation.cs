// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Diagnostics;
using Cratis.Arc.Validation;

namespace Cratis.Arc.Commands;

/// <summary>
/// Represents what one run of the command pipeline was asked to do.
/// </summary>
/// <param name="Command">The command instance.</param>
/// <param name="Services">The services the command runs with.</param>
/// <param name="AllowedSeverity">The validation severity threshold the caller asked for.</param>
/// <param name="CancellationToken">The request cancellation token.</param>
internal readonly record struct CommandInvocation(object Command, IServiceProvider Services, ValidationResultSeverity? AllowedSeverity, CancellationToken CancellationToken)
{
    /// <summary>
    /// Gets the span the run is traced in, if any.
    /// </summary>
    public Activity? Activity { get; init; }

    /// <summary>
    /// Gets the runtime type of the command.
    /// </summary>
    public Type CommandType => Command.GetType();
}
