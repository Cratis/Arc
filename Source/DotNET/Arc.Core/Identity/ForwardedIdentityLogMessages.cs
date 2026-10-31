// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Extensions.Logging;

namespace Cratis.Arc.Identity;

/// <summary>
/// Emits value-free signed forwarding rejection diagnostics.
/// </summary>
internal static partial class ForwardedIdentityLogMessages
{
    /// <summary>
    /// Reports a fixed protocol rejection code without credential material.
    /// </summary>
    /// <param name="logger">The host logger.</param>
    /// <param name="reason">A fixed reason code.</param>
    [LoggerMessage(LogLevel.Debug, "Signed forwarded identity rejected: {Reason}")]
    internal static partial void Rejected(ILogger logger, string reason);
}
