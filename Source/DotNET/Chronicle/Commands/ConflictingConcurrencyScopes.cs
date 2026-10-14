// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Chronicle.Commands;

/// <summary>
/// The exception that is thrown when an explicit concurrency intent conflicts with generated-target freshness.
/// </summary>
public class ConflictingConcurrencyScopes() : Exception("ConflictingConcurrencyScopes: a guarded generated target requires a whole-source BeforeFirst scope.");
