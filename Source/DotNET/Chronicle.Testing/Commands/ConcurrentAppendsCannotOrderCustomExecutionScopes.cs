// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Chronicle.Testing.Commands;

/// <summary>
/// The exception that is thrown when custom execution scope registrations prevent ordering competing appends before the owner commits.
/// </summary>
public class ConcurrentAppendsCannotOrderCustomExecutionScopes() : Exception("Competing appends cannot safely order custom execution scope registrations around the command transaction owner.");
