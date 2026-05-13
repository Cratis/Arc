// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Extensions.Logging;

namespace Cratis.Arc.Chronicle.ReadModels;

internal static partial class ReadModelInterceptorLogging
{
    [LoggerMessage(LogLevel.Trace, "Intercepting read model release for type '{ReadModelType}'")]
    internal static partial void InterceptingReadModel(this ILogger logger, string readModelType);

    [LoggerMessage(LogLevel.Trace, "Intercepted read model release for type '{ReadModelType}'")]
    internal static partial void InterceptedReadModel(this ILogger logger, string readModelType);
}