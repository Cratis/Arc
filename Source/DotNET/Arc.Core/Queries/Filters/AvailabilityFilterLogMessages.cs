// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Extensions.Logging;

namespace Cratis.Arc.Queries.Filters;

internal static partial class AvailabilityFilterLogMessages
{
    [LoggerMessage(LogLevel.Error, "Availability of query '{Query}' could not be determined; the request is refused as a retryable error")]
    internal static partial void AvailabilityNotDecided(this ILogger<AvailabilityFilter> logger, string query, Exception exception);
}
