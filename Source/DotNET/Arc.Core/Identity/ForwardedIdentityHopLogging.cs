// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Security.Cryptography;
using Cratis.Arc.Http;
using Microsoft.Extensions.Logging;

namespace Cratis.Arc.Identity;

/// <summary>
/// Bounds hostile-peer diagnostics without retaining any forwarded header values.
/// </summary>
/// <param name="clock">The warning rate-limit clock.</param>
internal sealed class ForwardedIdentityHopLogging(TimeProvider clock)
{
    /// <summary>The process-wide warning limiter.</summary>
    internal static readonly ForwardedIdentityHopLogging Shared = new(TimeProvider.System);
    readonly object _gate = new();
    readonly Dictionary<ForwardedIdentityHopAdmission, (DateTimeOffset Last, long Suppressed)> _warnings = [];

    /// <summary>Reports at most one warning per reason per minute.</summary>
    /// <param name="logger">The reader logger.</param>
    /// <param name="reason">The refusal reason.</param>
    /// <param name="request">The refused request.</param>
    internal void Report(ILogger logger, ForwardedIdentityHopAdmission reason, IHttpRequestContext request)
    {
        lock (_gate)
        {
            var now = clock.GetUtcNow();
            var previous = _warnings.GetValueOrDefault(reason);
            if (_warnings.ContainsKey(reason) && now - previous.Last < TimeSpan.FromMinutes(1))
            {
                _warnings[reason] = (previous.Last, previous.Suppressed + 1);
                return;
            }
            using var certificate = (request as ICanReadClientCertificate)?.TransportClientCertificate;
            logger.HopRefused(reason, certificate?.GetCertHashString(HashAlgorithmName.SHA256) ?? "none", certificate?.Subject ?? "none", previous.Suppressed);
            _warnings[reason] = (now, 0);
        }
    }
}
