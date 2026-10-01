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
}
