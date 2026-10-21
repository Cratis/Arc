// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Commands;

/// <summary>
/// The exception that is thrown when a command explicitly enables read protection and declares itself unprotected.
/// </summary>
/// <param name="commandType">The command with conflicting declarations.</param>
public class ConflictingReadProtection(Type commandType) : Exception($"Command '{commandType}' declares both ReadProtection On or Measure and Unprotected. Remove one of these declarations.");
