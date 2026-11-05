// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Http;
using Microsoft.Extensions.Logging;

namespace Cratis.Arc.Identity;

/// <summary>
/// Bounds forwarding diagnostics across the request and identity-discovery flows.
/// </summary>
/// <param name="clock">The refusal rate-limit clock.</param>
internal sealed class ForwardedIdentityDiagnostics(TimeProvider clock)
{
    /// <summary>The process-wide diagnostics state.</summary>
    internal static readonly ForwardedIdentityDiagnostics Shared = new(TimeProvider.System);
    readonly object _gate = new();
    readonly HashSet<(string Flow, string Form)> _accepted = [];
    readonly Dictionary<(string Flow, string Reason), DateTimeOffset> _refused = [];

    /// <summary>Reports the first accepted forwarding form for each flow.</summary>
    /// <param name="logger">The reader logger.</param>
    /// <param name="request">The accepted request.</param>
    /// <param name="form">The fixed forwarding form name.</param>
    internal void Accepted(ILogger logger, IHttpRequestContext request, string form)
    {
        lock (_gate)
        {
            var flow = Flow(request);
            if (_accepted.Add((flow, form))) logger.ForwardingAccepted(flow, form);
        }
    }

    /// <summary>Reports at most one refusal per flow and fixed reason per minute.</summary>
    /// <param name="logger">The reader logger.</param>
    /// <param name="request">The refused request.</param>
    /// <param name="reason">The fixed refusal reason, never a header value.</param>
    internal void Refused(ILogger logger, IHttpRequestContext request, string reason)
    {
        lock (_gate)
        {
            var key = (Flow(request), reason);
            var now = clock.GetUtcNow();
            if (_refused.TryGetValue(key, out var previous) && now - previous < TimeSpan.FromMinutes(1)) return;
            _refused[key] = now;
            logger.ForwardingRefused(key.Item1, reason);
        }
    }

    static string Flow(IHttpRequestContext request) => request.Path.TrimEnd('/') switch
    {
        "/.cratis/me" => "IdentityDetails",
        "/.cratis/tenants" => "TenantList",
        _ => "Requests"
    };
}
