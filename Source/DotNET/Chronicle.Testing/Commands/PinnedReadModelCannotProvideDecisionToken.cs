// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Chronicle.Testing.Commands;

/// <summary>
/// The exception that is thrown when a read model is pinned in a scenario in decision mode.
/// </summary>
public class PinnedReadModelCannotProvideDecisionToken() : Exception("Pinned read models cannot provide protected decision tokens. Seed events into the decision-mode log instead.");
