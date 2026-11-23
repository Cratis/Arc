// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Runtime.CompilerServices;

namespace Cratis.Arc.Contexts;

/// <summary>
/// Carries values explicitly attached by a bound command or query into its existing validation snapshot.
/// A snapshot without an attachment never infers an invocation from services, caller arguments or ambient state.
/// </summary>
internal static class InvocationContextValues
{
    static readonly ConditionalWeakTable<OperationSnapshot, ContextValues> _values = new();

    /// <summary>Attaches the explicitly bound operation's values to its validation snapshot.</summary>
    /// <param name="snapshot">The snapshot of the operation that owns these values.</param>
    /// <param name="values">The lazy readers for its bound invocation.</param>
    internal static void Attach(OperationSnapshot snapshot, ContextValues values) => _values.AddOrUpdate(snapshot, values);

    /// <summary>Gets explicitly attached values without inferring an invocation for an unattached snapshot.</summary>
    /// <param name="snapshot">The snapshot used by the validator.</param>
    /// <returns>The bound readers, or null when the snapshot has no explicit invocation.</returns>
    internal static ContextValues? For(OperationSnapshot snapshot) => _values.TryGetValue(snapshot, out var values) ? values : null;
}
