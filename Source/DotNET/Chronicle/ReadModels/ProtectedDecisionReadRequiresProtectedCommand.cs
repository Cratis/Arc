// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Chronicle.ReadModels;

/// <summary>
/// The exception that is thrown when a protected decision read is attempted in a command that is not marked <c>[ProtectedDecision]</c>.
/// </summary>
public class ProtectedDecisionReadRequiresProtectedCommand() : Exception("Protected decision reads require [ProtectedDecision] on the command. Mark intentional advisory reads [Unprotected].");
