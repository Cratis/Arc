// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Extensions.Logging;

namespace Cratis.Arc.Identity;

/// <summary>
/// Startup guidance for host-owned TLS configuration.
/// </summary>
internal static partial class ForwardedIdentityHopStartupValidationLogging
{
    /// <summary>
    /// Reports the listener prerequisite without changing the host's TLS policy.
    /// </summary>
    /// <param name="logger">The startup logger.</param>
    [LoggerMessage(LogLevel.Warning, "Forwarded identity requires a transport client certificate. Configure each Kestrel HTTPS listener with AllowCertificate or RequireCertificate and an appropriate client-certificate validation policy. Arc does not change HTTPS defaults or validation callbacks. Without a trusted transport certificate, forwarded identity is refused.")]
    internal static partial void HostMustRequestClientCertificates(this ILogger<ForwardedIdentityHopStartupValidation> logger);
}
