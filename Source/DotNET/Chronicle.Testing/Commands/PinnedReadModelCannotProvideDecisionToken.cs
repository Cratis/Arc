// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Chronicle.Testing.Commands;

/// <summary>
/// The exception that is thrown when a scenario in decision mode pins a read model that the command reads as a protected decision read.
/// </summary>
public class PinnedReadModelCannotProvideDecisionToken : Exception
{
    /// <summary>
    /// Initializes a new instance of the <see cref="PinnedReadModelCannotProvideDecisionToken"/> class.
    /// </summary>
    public PinnedReadModelCannotProvideDecisionToken()
        : base("Pinned read models cannot provide protected decision tokens. Seed events into the decision-mode log instead.")
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="PinnedReadModelCannotProvideDecisionToken"/> class.
    /// </summary>
    /// <param name="commandType">The protected command.</param>
    /// <param name="readModelType">The pinned read model type.</param>
    public PinnedReadModelCannotProvideDecisionToken(Type commandType, Type readModelType)
        : base($"'{commandType.Name}' reads '{readModelType.Name}' as a protected decision read, which always folds the event log and never sees a pinned instance. " +
            "Seed the events it is projected from with Given.ForEventSource(...).Events(...) instead.")
    {
    }
}
