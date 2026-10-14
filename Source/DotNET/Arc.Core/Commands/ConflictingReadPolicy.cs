// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Commands;

/// <summary>
/// The exception that is thrown when one read-policy level declares both Unprotected and Consistency.
/// </summary>
/// <param name="commandType">The command type.</param>
public class ConflictingReadPolicy(Type commandType) : Exception($"Command '{commandType}' declares both Unprotected and Consistency on the same read-policy level.");
