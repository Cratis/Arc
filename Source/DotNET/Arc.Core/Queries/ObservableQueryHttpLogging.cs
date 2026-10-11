// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Execution;
using Microsoft.Extensions.Logging;

namespace Cratis.Arc.Queries;

internal static partial class ObservableQueryHttpLogging
{
    [LoggerMessage(Level = LogLevel.Error, Message = "An observable HTTP query failed (correlation id: {CorrelationId}).")]
    internal static partial void ObservableHttpQueryFailed(this ILogger logger, Exception exception, CorrelationId correlationId);
}
