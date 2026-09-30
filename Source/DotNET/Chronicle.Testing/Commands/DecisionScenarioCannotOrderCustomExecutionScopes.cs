// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Chronicle.Testing.Commands;

/// <summary>
/// The exception that is thrown when a decision scenario is used together with custom command execution scopes.
/// </summary>
public class DecisionScenarioCannotOrderCustomExecutionScopes() : Exception("Decision scenarios cannot order custom execution scopes safely. Use a host integration test for that scope combination.");
