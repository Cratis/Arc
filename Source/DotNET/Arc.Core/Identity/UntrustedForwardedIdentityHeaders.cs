// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Extensions.Logging;

namespace Cratis.Arc.Identity;

/// <summary>
/// Reports, once per process, that a request carried forwarded identity headers the host does not trust.
/// </summary>
internal static class UntrustedForwardedIdentityHeaders
{
    static int _reported;

    /// <summary>
    /// Checks whether the request carries any of the forwarded identity headers.
    /// </summary>
    /// <param name="hasHeader">Checks whether the request has a header with the given name.</param>
    /// <returns>True if any forwarded identity header is present.</returns>
    internal static bool ArePresent(Func<string, bool> hasHeader) =>
        hasHeader(MicrosoftIdentityPlatformHeaders.PrincipalHeader) ||
        hasHeader(MicrosoftIdentityPlatformHeaders.IdentityIdHeader) ||
        hasHeader(MicrosoftIdentityPlatformHeaders.IdentityNameHeader);

    /// <summary>
    /// Logs a warning that forwarded identity headers were ignored. Only the first occurrence in the process is logged.
    /// </summary>
    /// <param name="logger">The logger to report to.</param>
    internal static void Report(ILogger logger)
    {
        if (Interlocked.Exchange(ref _reported, 1) == 0)
        {
            logger.ForwardedIdentityHeadersIgnored();
        }
    }
}
