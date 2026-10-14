// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Commands;

/// <summary>
/// The exception that is thrown when a protected or unprotected command explicitly enables the key guard.
/// </summary>
/// <param name="commandType">The command type.</param>
/// <param name="reason">The conflicting protection declaration.</param>
public class CommandCannotCombineProtectedDecisionAndKeyGuard(Type commandType, string reason) : Exception($"Command '{commandType}' cannot combine {reason} with KeyGuard On or Observe.");
