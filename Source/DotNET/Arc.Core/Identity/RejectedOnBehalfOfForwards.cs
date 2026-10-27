// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Extensions.Logging;

namespace Cratis.Arc.Identity;

/// <summary>
/// Reports rejected on-behalf forwards: the first in the process as a warning, every later one at debug level.
/// </summary>
/// <remarks>
/// Anyone who can reach the host can send the on-behalf headers, so a warning per rejected request would let a caller
/// flood the logs.
/// </remarks>
internal static class RejectedOnBehalfOfForwards
{
    static int _reported;

    /// <summary>
    /// Reports a rejected on-behalf forward.
    /// </summary>
    /// <param name="logger">The logger to report to.</param>
    /// <param name="reason">Why the forward was rejected.</param>
    internal static void Report(ILogger logger, string reason) => Report(logger, reason, ref _reported);

    /// <summary>
    /// Reports a rejected on-behalf forward using the supplied reporting state.
    /// </summary>
    /// <param name="logger">The logger to report to.</param>
    /// <param name="reason">Why the forward was rejected.</param>
    /// <param name="reported">The shared reporting state.</param>
    internal static void Report(ILogger logger, string reason, ref int reported)
    {
        if (Interlocked.Exchange(ref reported, 1) == 0)
        {
            logger.OnBehalfOfForwardRejected(reason);
            return;
        }

        logger.OnBehalfOfForwardRejectedAgain(reason);
    }
}
