// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Extensions.Logging;

namespace Cratis.Arc.Identity;

#pragma warning disable SA1600 // Elements should be documented
#pragma warning disable MA0048 // File name must match type name

internal static partial class UntrustedForwardedIdentityHeadersLogMessages
{
    [LoggerMessage(LogLevel.Warning, "A request carried forwarded identity headers (x-ms-client-principal*), but this host does not trust them, so they were ignored and the request was treated as anonymous. If every request reaches this host through an ingress that strips and sets these headers (Azure App Service or Container Apps authentication, or Cratis AuthProxy), opt in with options.TrustForwardedIdentityHeaders = true or Cratis:Arc:TrustForwardedIdentityHeaders=true. See https://github.com/Cratis/Arc/blob/main/Documentation/upgrading/secure-defaults.md. This is reported once per process.")]
    internal static partial void ForwardedIdentityHeadersIgnored(this ILogger logger);

    [LoggerMessage(LogLevel.Warning, "The Microsoft Identity Platform authentication scheme is registered, but this host does not trust forwarded identity headers (x-ms-client-principal*), so every request is anonymous and endpoints that require authentication return 401. If every request reaches this host through an ingress that strips and sets these headers (Azure App Service or Container Apps authentication, or Cratis AuthProxy), opt in with options.TrustForwardedIdentityHeaders = true or Cratis:Arc:TrustForwardedIdentityHeaders=true. See https://github.com/Cratis/Arc/blob/main/Documentation/upgrading/secure-defaults.md.")]
    internal static partial void ForwardedIdentityHeadersNotTrustedAtStartup(this ILogger logger);

    [LoggerMessage(LogLevel.Warning, "A request carried on-behalf headers (x-cratis-on-behalf-actor or x-cratis-on-behalf-assertion) and was rejected: {Reason}. On-behalf forwards are accepted only with Cratis:Arc:OnBehalfOf:Enabled and a verified assertion. See https://github.com/Cratis/Arc/blob/main/Documentation/backend/csharp/asp-net-core/acting-on-behalf-of.md. This is reported as a warning once per process; later rejections are logged at debug level.")]
    internal static partial void OnBehalfOfForwardRejected(this ILogger logger, string reason);

    [LoggerMessage(LogLevel.Error, "On-behalf forwarding is enabled (Cratis:Arc:OnBehalfOf:Enabled), but its configuration cannot be used: {Problem}. Every request carrying on-behalf headers will fail authentication until it is fixed.")]
    internal static partial void OnBehalfOfMisconfiguredAtStartup(this ILogger logger, string problem);

    [LoggerMessage(LogLevel.Debug, "A request carrying on-behalf headers was rejected: {Reason}")]
    internal static partial void OnBehalfOfForwardRejectedAgain(this ILogger logger, string reason);
}
