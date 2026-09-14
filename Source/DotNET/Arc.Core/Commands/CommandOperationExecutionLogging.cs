// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Extensions.Logging;

namespace Cratis.Arc.Commands;

internal static partial class CommandOperationExecutionLogging
{
    [LoggerMessage(Level = LogLevel.Warning, Message = "Command {CorrelationId} compensation failed for invocation {InvocationIndex} ({OperationType})")]
    internal static partial void CompensationFailed(this ILogger logger, string correlationId, int invocationIndex, string operationType, Exception exception);

    [LoggerMessage(Level = LogLevel.Information, Message = "Command {CorrelationId} operation recovery: commit {CommitDisposition}, status {RecoveryStatus}, started {StartedCount}, compensated {CompensatedCount}, unresolved {UncompensatedCount}")]
    internal static partial void RecoveryObserved(this ILogger logger, string correlationId, CommandCommitDisposition commitDisposition, CommandRecoveryStatus recoveryStatus, int startedCount, int compensatedCount, int uncompensatedCount);
}
