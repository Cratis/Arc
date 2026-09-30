// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Chronicle.ReadModels;

/// <summary>
/// The exception that is thrown when a protected command does not receive a directly issued <c>DecisionRead&lt;T&gt;</c> token.
/// </summary>
public class ProtectedCommandRequiresIssuedDecisionRead() : Exception("A protected command requires a directly issued DecisionRead<T> token.");
