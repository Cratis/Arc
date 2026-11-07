// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Commands;

namespace Cratis.Arc.Identity.Approvals;

/// <summary>A test-only proxy base for the internal approval-flow seam.</summary>
public class ApprovalFlowForSpecs : IApprovalFlow
{
    /// <inheritdoc/>
    public Task<CommandResult> Begin(CommandContext context) => Start(context);

    /// <summary>Supplies the approval result configured by a spec.</summary>
    /// <param name="context">The command context.</param>
    /// <returns>The approval result.</returns>
    public virtual Task<CommandResult> Start(CommandContext context) => Task.FromResult(new CommandResult());
}
