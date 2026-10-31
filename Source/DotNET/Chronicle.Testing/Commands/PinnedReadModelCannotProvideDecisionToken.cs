// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Chronicle.Testing.Commands;

/// <summary>
/// The exception that is thrown when a scenario pins a read model the command reads as a protected decision token.
/// </summary>
public class PinnedReadModelCannotProvideDecisionToken : Exception
{
    const string Guidance = "A pinned read model cannot provide a decision token for DecisionRead<T>. " +
        "Take the read model as a plain Handle parameter under [ReadProtection(On)], then pin it, or give the events that build it.";

    /// <summary>
    /// Initializes a new instance of the <see cref="PinnedReadModelCannotProvideDecisionToken"/> class.
    /// </summary>
    public PinnedReadModelCannotProvideDecisionToken()
        : base(Guidance)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="PinnedReadModelCannotProvideDecisionToken"/> class.
    /// </summary>
    /// <param name="commandType">The protected command.</param>
    /// <param name="readModelType">The pinned read model type.</param>
    public PinnedReadModelCannotProvideDecisionToken(Type commandType, Type readModelType)
        : base($"'{commandType.Name}' reads '{readModelType.Name}' as a protected decision read. {Guidance}")
    {
    }
}
