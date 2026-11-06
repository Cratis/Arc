// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Extensions.Logging;

namespace Cratis.Arc.Identity.Approvals;

internal static partial class ApprovalGateLogging
{
    [LoggerMessage(LogLevel.Warning, "No approval flow is registered for command {CommandType}.")]
    internal static partial void ApprovalRequiredWithoutPackage(this ILogger logger, string commandType);

    [LoggerMessage(LogLevel.Error, "Multiple approval flows are registered for command {CommandType}.")]
    internal static partial void AmbiguousApprovalFlows(this ILogger logger, string commandType);

    /// <summary>
    /// Logs only the command and exception types, without exception messages or command contents.
    /// </summary>
    /// <param name="logger">The logger to use.</param>
    /// <param name="exception">The failure whose type is logged.</param>
    /// <param name="commandType">The command type name.</param>
    internal static void ApprovalFlowFailed(this ILogger logger, Exception exception, string commandType) =>
        logger.ApprovalFlowFailed(commandType, exception.GetType().FullName ?? exception.GetType().Name);

    [LoggerMessage(LogLevel.Error, "Approval flow failed for command {CommandType} with exception type {ExceptionType}.")]
    static partial void ApprovalFlowFailed(this ILogger logger, string commandType, string exceptionType);
}
