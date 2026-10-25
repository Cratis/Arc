// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.ReadModels;
using Microsoft.Extensions.Logging;

namespace Cratis.Arc.Chronicle.ReadModels;

internal static partial class DecisionReadAdmissionStartupLogging
{
    [LoggerMessage(Message = "Chronicle refuses the decision read of {ReadModel} for {Command} ({Reason}). Read a dedicated read model of the admitted shape instead, or keep the command's existing concurrency handling for that state.")]
    internal static partial void ReadRefused(this ILogger<DecisionReadAdmissionStartup> logger, LogLevel level, string command, string readModel, DecisionReadRefusalReason reason);

    [LoggerMessage(Level = LogLevel.Debug, Message = "Decision-read startup admission check skipped: Chronicle discovery or the event store is unavailable.")]
    internal static partial void AdmissionSkipped(this ILogger<DecisionReadAdmissionStartup> logger);

    [LoggerMessage(Level = LogLevel.Warning, Message = "Decision-read startup admission check could not complete. Admission will still be checked when the command executes.")]
    internal static partial void AdmissionFailed(this ILogger<DecisionReadAdmissionStartup> logger, Exception exception);
}
