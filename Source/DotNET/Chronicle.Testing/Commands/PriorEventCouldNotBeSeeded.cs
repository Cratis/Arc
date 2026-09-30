// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Chronicle.Testing.Commands;

/// <summary>
/// The exception that is thrown when a prior event could not be seeded into the decision scenario log.
/// </summary>
public class PriorEventCouldNotBeSeeded() : Exception("A prior event could not be seeded.");
