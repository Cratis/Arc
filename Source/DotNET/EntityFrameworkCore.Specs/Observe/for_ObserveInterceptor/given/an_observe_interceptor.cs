// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Extensions.Logging;

namespace Cratis.Arc.EntityFrameworkCore.Observe.for_ObserveInterceptor.given;

public class an_observe_interceptor : Specification
{
    protected IEntityChangeTracker _changeTracker;
    protected ILogger<ObserveInterceptor> _logger;
    protected ObserveInterceptor _interceptor;

    void Establish()
    {
        _changeTracker = Substitute.For<IEntityChangeTracker>();
        _logger = Substitute.For<ILogger<ObserveInterceptor>>();
        _interceptor = new ObserveInterceptor(_changeTracker, _logger);
    }
}
