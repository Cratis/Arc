// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Chronicle.Testing.Commands;

/// <summary>
/// The exception that is thrown when competing appends cannot find a discovered transactional command scope.
/// </summary>
public class ConcurrentAppendsRequireTransactionalCommandScope() : Exception("Competing appends require a discovered TransactionalCommandScope.");
