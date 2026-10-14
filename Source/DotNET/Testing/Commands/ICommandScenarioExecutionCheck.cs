// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Testing.Commands;

/// <summary>
/// Checks scenario setup before entering the command pipeline.
/// </summary>
internal interface ICommandScenarioExecutionCheck
{
    /// <summary>
    /// Verifies that the scenario can prove the behavior configured for the command.
    /// </summary>
    /// <param name="commandType">The command to execute.</param>
    /// <param name="fixture">The scenario's generated value pins.</param>
    void Verify(Type commandType, ScenarioGeneratedValueFixture fixture);
}
