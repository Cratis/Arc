// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Testing.for_QueryScenario;

public class StreamInvocationTracker
{
    int _invocations;

    public int Invocations => Volatile.Read(ref _invocations);

    public void RecordInvocation() => Interlocked.Increment(ref _invocations);
}
