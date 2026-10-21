// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Observability;

namespace Cratis.Arc.Queries.for_ClientObservableSSE.when_handling_connection.given;

public class a_telemetered_client_observable_sse : a_guarded_client_observable_sse
{
    internal ObservedQueryServices _observed;

    void Establish()
    {
        _observed = new ObservedQueryServices([QueryName]);
        _context.RequestServices.Returns(_observed.Services);
        _emissionGuards.HasGuards.Returns(false);
    }

    void Destroy() => _observed.Dispose();
}
