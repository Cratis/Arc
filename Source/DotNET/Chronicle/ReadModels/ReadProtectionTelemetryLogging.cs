// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Extensions.Logging;

namespace Cratis.Arc.Chronicle.ReadModels;

internal static partial class ReadProtectionTelemetryLogging
{
    [LoggerMessage(LogLevel.Debug, "Read-protection hypothesis for {Command}: category={Category}, model={Model}, reason={Reason}")]
    internal static partial void Hypothesis(this ILogger logger, string? command, string category, string? model, string? reason);

    [LoggerMessage(LogLevel.Warning, "Read-protection hypothesis for {Command}: category={Category}, model={Model}, reason={Reason}; this may need attention before turning On")]
    internal static partial void RolloutWarning(this ILogger logger, string? command, string category, string? model, string? reason);

    [LoggerMessage(LogLevel.Information, "Read-protection measurement summary: {Observations} observations, {InstrumentationFailures} instrumentation failures; observations are hypotheses, not guarantees")]
    internal static partial void Summary(this ILogger logger, long observations, long instrumentationFailures);

    [LoggerMessage(LogLevel.Warning, "Read-protection measurement unavailable for {Command}: {Reason}")]
    internal static partial void Unavailable(this ILogger logger, string? command, string reason);
}
