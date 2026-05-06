// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Extensions.Logging;

namespace TestApps.Maui.Services;

/// <summary>
/// High-performance log messages for <see cref="ArcHostService"/>.
/// </summary>
internal static partial class ArcHostServiceLogging
{
    [LoggerMessage(Level = LogLevel.Warning, Message = "Arc backend is already running.")]
    internal static partial void AlreadyRunning(this ILogger logger);

    [LoggerMessage(Level = LogLevel.Information, Message = "Starting Arc backend on port {Port}.")]
    internal static partial void Starting(this ILogger logger, int port);

    [LoggerMessage(Level = LogLevel.Information, Message = "Arc backend started on port {Port}.")]
    internal static partial void Started(this ILogger logger, int port);

    [LoggerMessage(Level = LogLevel.Information, Message = "Stopping Arc backend.")]
    internal static partial void Stopping(this ILogger logger);

    [LoggerMessage(Level = LogLevel.Warning, Message = "Arc backend stop timed out or failed.")]
    internal static partial void StopFailed(this ILogger logger, Exception exception);

    [LoggerMessage(Level = LogLevel.Information, Message = "Arc backend stopped.")]
    internal static partial void Stopped(this ILogger logger);
}
