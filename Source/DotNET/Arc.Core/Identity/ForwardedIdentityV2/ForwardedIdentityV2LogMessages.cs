// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Extensions.Logging;

namespace Cratis.Arc.Identity.ForwardedIdentityV2;

internal static partial class ForwardedIdentityV2LogMessages
{
    [LoggerMessage(LogLevel.Debug, "Forwarded identity v2 refused: {Reason}")]
    internal static partial void Rejected(this ILogger logger, ForwardedIdentityV2Rejection reason);
}
