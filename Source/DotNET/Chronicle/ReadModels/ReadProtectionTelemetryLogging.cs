// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Extensions.Logging;

namespace Cratis.Arc.Chronicle.ReadModels;

internal static partial class ReadProtectionTelemetryLogging
{
    [LoggerMessage(LogLevel.Debug, "Read-protection hypothesis for {Command}: category={Category}, model={Model}, reason={Reason}")]
    internal static partial void Hypothesis(this ILogger logger, string? command, string category, string? model, string? reason);

    [LoggerMessage(LogLevel.Warning, "Read-protection measurement unavailable for {Command}: {Reason}")]
    internal static partial void Unavailable(this ILogger logger, string? command, string reason);
}
