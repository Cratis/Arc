// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.EventSequences.Concurrency;

namespace Cratis.Arc.Chronicle.Commands;

/// <summary>
/// A guard derived for one event of a command, together with where it came from.
/// </summary>
/// <param name="Scope">The <see cref="ConcurrencyScope"/> to attach, or null when the event sequence should apply its own.</param>
/// <param name="Origin">The <see cref="DerivedConcurrencyScopeOrigin"/> of the guard.</param>
internal record DerivedConcurrencyScope(ConcurrencyScope? Scope, DerivedConcurrencyScopeOrigin Origin);
