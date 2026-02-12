// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Extensions.Logging;

namespace Cratis.Arc.Http;

/// <summary>
/// Log messages for <see cref="TenantIdMiddleware"/>.
/// </summary>
internal static partial class TenantIdMiddlewareLogMessages
{
    [LoggerMessage(LogLevel.Debug, "Setting tenant ID to {TenantId}")]
    internal static partial void SettingTenantId(this ILogger<TenantIdMiddleware> logger, string tenantId);
}
