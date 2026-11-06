// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Capabilities.for_CapabilityHosting;

public class ScopedControllerProbe
{
    int _invocations;

    public int Invocations => _invocations;

    public void Ran() => Interlocked.Increment(ref _invocations);
}
