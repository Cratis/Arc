// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Chronicle.Testing.Commands;

/// <summary>
/// The exception that is thrown when routed event seeding is requested for legacy read-model state.
/// </summary>
public class RoutedEventSeedingRequiresDecisionReads()
    : Exception("Routed event seeding needs a command that declares decision dependencies, or give the event on its own event source.");
