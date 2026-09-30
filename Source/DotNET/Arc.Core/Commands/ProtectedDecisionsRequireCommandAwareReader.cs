// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Commands;

/// <summary>
/// The exception that is thrown when a protected command is executed with a provider that cannot supply command-aware decision reads.
/// </summary>
public class ProtectedDecisionsRequireCommandAwareReader() : Exception("Protected decisions require a command-aware Chronicle decision reader in the command provider.");
