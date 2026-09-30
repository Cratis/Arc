// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Commands;

/// <summary>
/// The exception that is thrown when a command declares both the protected and the unprotected decision profile.
/// </summary>
/// <param name="commandType">The command type that declares both profiles.</param>
public class CommandCannotBeBothProtectedAndUnprotected(Type commandType) : Exception($"Command '{commandType}' cannot be both protected and unprotected.");
