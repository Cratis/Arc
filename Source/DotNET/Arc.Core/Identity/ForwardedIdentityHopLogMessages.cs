// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Extensions.Logging;

namespace Cratis.Arc.Identity;

internal static partial class ForwardedIdentityHopLogMessages
{
    [LoggerMessage(LogLevel.Warning, "Forwarded identity refused: {Reason}; certificate {Fingerprint} ({Subject}); suppressed {Suppressed}")]
    internal static partial void HopRefused(this ILogger logger, ForwardedIdentityHopAdmission reason, string fingerprint, string subject, long suppressed);

    [LoggerMessage(LogLevel.Warning, "Forwarded identity trust bundle replacement unavailable at {Path}, SHA-256 {Hash}; retaining last good bundle")]
    internal static partial void TrustBundleUnavailable(this ILogger logger, string path, string hash);
}
