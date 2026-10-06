// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Extensions.Logging;

namespace Cratis.Arc.Introspection;

#pragma warning disable SA1600 // Elements should be documented
#pragma warning disable MA0048 // File name must match type name

internal static partial class DiscoveryExposureLogMessages
{
    [LoggerMessage(LogLevel.Warning, "The discovery endpoints ({Endpoints}) are exposed anonymously outside Development because Cratis:Arc:Introspection:RequireAuthentication is false. Remove the setting to require authenticated callers.")]
    internal static partial void DiscoveryExposedAnonymously(this ILogger logger, string endpoints);

    [LoggerMessage(LogLevel.Warning, "The discovery endpoints ({Endpoints}) are not mapped, because outside Development they require authenticated callers and the host cannot authenticate them: {Reason} Configure authentication, or set Cratis:Arc:Introspection:RequireAuthentication to false to expose them anonymously.")]
    internal static partial void DiscoveryNotMapped(this ILogger logger, string endpoints, string reason);
}
