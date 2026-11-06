// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace VerifiedCallerAcceptance;

/// <summary>
/// Records dispatches so the specs can distinguish a refusal from an invoked operation.
/// </summary>
public sealed class InvocationCounts
{
    int _publishes;

    /// <summary>Gets the number of publish operations invoked.</summary>
    public int Publishes => Volatile.Read(ref _publishes);

    /// <summary>Records a publish dispatch.</summary>
    public void Published() => Interlocked.Increment(ref _publishes);
}
