// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Extensions.Logging;

namespace Cratis.Arc.Identity;

internal static partial class ForwardedIdentityHopLogMessages
{
    [LoggerMessage(0, LogLevel.Warning, "Forwarded identity refused: hop")]
    internal static partial void HopRefused(this ILogger logger);

    [LoggerMessage(1, LogLevel.Warning, "Forwarded identity trust bundle unavailable")]
    internal static partial void TrustBundleUnavailable(this ILogger logger);
}
