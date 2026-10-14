// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Chronicle.Testing.Commands;

/// <summary>
/// The exception that is thrown when a scenario seeds a pinned guarded identifier without enabling decision reads.
/// </summary>
public sealed class GeneratedIdentifierCollisionRequiresDecisionReads() : Exception(
    "The scenario cannot prove a generated-identifier collision without UseDecisionReads(). " +
    "Create the scenario with new CommandScenario<TCommand>().UseDecisionReads() before seeding events and pinning the generated value.");
