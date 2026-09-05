// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Extensions.Logging;

namespace Cratis.Arc;

/// <summary>
/// Log messages for <see cref="ExceptionDetailRedactor"/>.
/// </summary>
internal static partial class ExceptionDetailRedactorLogMessages
{
    [LoggerMessage(LogLevel.Error, "An unhandled exception occurred while processing a request (correlation id: {CorrelationId}). Messages: {ExceptionMessages}. Stack trace: {ExceptionStackTrace}")]
    internal static partial void RedactedExceptionDetail(this ILogger logger, string correlationId, string exceptionMessages, string exceptionStackTrace);
}
