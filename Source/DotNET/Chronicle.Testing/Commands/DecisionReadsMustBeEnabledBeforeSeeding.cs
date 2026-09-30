// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Chronicle.Testing.Commands;

/// <summary>
/// The exception that is thrown when decision reads are enabled after legacy event scenario or read model state has been seeded.
/// </summary>
public class DecisionReadsMustBeEnabledBeforeSeeding() : Exception("Enable decision reads before seeding legacy EventScenario or read model state.");
