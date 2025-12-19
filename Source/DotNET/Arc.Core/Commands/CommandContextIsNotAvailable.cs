// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Commands;

/// <summary>
/// Exception that is thrown when no command context is available in the current scope.
/// </summary>
public class CommandContextIsNotAvailable() : Exception("No command context is available in the current scope.");