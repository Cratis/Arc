// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Observability;

namespace Cratis.Arc.Queries.for_ClientEnumerableObservable.when_handling_connection.given;

public class a_telemetered_client_enumerable_observable : a_guarded_client_enumerable_observable
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
