// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Queries.for_WebSocketOriginGate;

public class OriginProbe
{
    int _queries;

    public int Queries => _queries;

    public void Ran() => Interlocked.Increment(ref _queries);
}
