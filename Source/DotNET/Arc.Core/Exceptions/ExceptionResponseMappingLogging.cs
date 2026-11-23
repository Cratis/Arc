// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Extensions.Logging;

namespace Cratis.Arc.Exceptions;

internal static partial class ExceptionResponseMappingLogging
{
    [LoggerMessage(Level = LogLevel.Warning, Message = "Exception response mapping failed; retaining the original failure response")]
    internal static partial void ResponseMappingFailed(this ILogger logger, Exception exception);

    [LoggerMessage(Level = LogLevel.Warning, Message = "Exception response mapper returned an invalid response; retaining the original failure response")]
    internal static partial void InvalidExceptionResponse(this ILogger logger);
}
