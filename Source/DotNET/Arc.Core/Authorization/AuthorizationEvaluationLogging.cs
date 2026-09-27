// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Extensions.Logging;

namespace Cratis.Arc.Authorization;

internal static partial class AuthorizationEvaluationLogging
{
    [LoggerMessage(LogLevel.Warning, "Guest authorization policy failed for '{Target}' - denying access")]
    internal static partial void GuestPolicyFailed(this ILogger<AuthorizationEvaluation> logger, string target, Exception exception);
}
