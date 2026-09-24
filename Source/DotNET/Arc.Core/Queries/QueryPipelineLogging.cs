// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Extensions.Logging;

namespace Cratis.Arc.Queries;

internal static partial class QueryPipelineLogging
{
    [LoggerMessage(Level = LogLevel.Error, Message = "Query authorization configuration failed")]
    internal static partial void AuthorizationConfigurationFailed(this ILogger<QueryPipeline> logger, Exception exception);

    [LoggerMessage(Level = LogLevel.Error, Message = "Query authorization preparation failed")]
    internal static partial void AuthorizationPreparationFailed(this ILogger<QueryPipeline> logger, Exception exception);
}
