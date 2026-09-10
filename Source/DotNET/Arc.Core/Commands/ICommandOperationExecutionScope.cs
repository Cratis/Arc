// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Commands;

/// <summary>
/// Explicitly opts an execution scope into flat command operation boundaries. Legacy scope order is preserved.
/// A nonparticipant promises not to commit business changes; a participant reports conservative commit facts even
/// after completion throws. At most one deferred participant is supported. Begin must not commit business work.
/// </summary>
public interface ICommandOperationExecutionScope : ICommandExecutionScope
{
    /// <summary>
    /// Gets whether this scope coordinates a deferred business commit.
    /// </summary>
    bool IsCommitParticipant { get; }

    /// <summary>
    /// Gets observed commit facts for this command, including uncertainty from out-of-band commits.
    /// </summary>
    /// <param name="context">The originating command context.</param>
    /// <returns>Authoritative facts, or Unknown when unavailable. Never infer this from command success alone.</returns>
    CommandCommitDisposition GetCommitDisposition(CommandContext context);
}
