// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Commands;

/// <summary>
/// The exception that is thrown when the current <see cref="CommandContext"/> is requested but none is available in the current scope.
/// </summary>
public class NoCommandContextAvailable()
    : Exception("No command context is available in the current scope. This typically happens when a command-scoped service, such as a read model, is resolved outside of an executing command.");
