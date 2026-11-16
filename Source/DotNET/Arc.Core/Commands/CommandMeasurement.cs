// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Diagnostics;

namespace Cratis.Arc.Commands;

/// <summary>
/// Represents what is needed to record the outcome of one command on its span and metrics.
/// </summary>
/// <param name="Activity">The active span, if any.</param>
/// <param name="Services">The request services.</param>
/// <param name="CommandType">The command type.</param>
/// <param name="Started">The start timestamp.</param>
/// <param name="CancellationToken">The request cancellation token.</param>
internal readonly record struct CommandMeasurement(Activity? Activity, IServiceProvider Services, Type CommandType, long Started, CancellationToken CancellationToken);
