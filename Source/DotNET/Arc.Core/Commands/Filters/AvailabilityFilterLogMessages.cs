// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Extensions.Logging;

namespace Cratis.Arc.Commands.Filters;

internal static partial class AvailabilityFilterLogMessages
{
    [LoggerMessage(LogLevel.Error, "Availability of command '{Part}' could not be determined; the request is refused as a retryable error")]
    internal static partial void AvailabilityNotDecided(this ILogger<AvailabilityFilter> logger, string part, Exception exception);
}
