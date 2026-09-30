// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Chronicle.ReadModels;

/// <summary>
/// The exception that is thrown when a protected command decision runs over a unit of work that cannot be owned by the command.
/// </summary>
public class ProtectedDecisionRequiresOwnerCapableUnitOfWork() : Exception("Protected command decisions require Chronicle's owner-capable UnitOfWork.");
