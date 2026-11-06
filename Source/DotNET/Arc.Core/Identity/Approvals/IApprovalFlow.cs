// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Commands;

namespace Cratis.Arc.Identity.Approvals;

/// <summary>
/// Initiates approval for a command instead of executing its handler.
/// The handler is never invoked by the request that begins the flow.
/// </summary>
internal interface IApprovalFlow
{
    /// <summary>
    /// Begins approval using the current command context.
    /// </summary>
    /// <param name="context">The authorized and validated command context.</param>
    /// <returns>The result sent to the caller as is.</returns>
    Task<CommandResult> Begin(CommandContext context);
}
