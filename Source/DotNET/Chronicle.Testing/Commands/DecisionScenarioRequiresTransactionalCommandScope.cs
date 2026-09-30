// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Chronicle.Testing.Commands;

/// <summary>
/// The exception that is thrown when a decision scenario is used without the transactional command scope.
/// </summary>
public class DecisionScenarioRequiresTransactionalCommandScope() : Exception("Decision scenarios require TransactionalCommandScope.");
