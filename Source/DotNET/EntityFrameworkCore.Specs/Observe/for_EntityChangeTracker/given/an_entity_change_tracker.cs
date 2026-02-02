// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Extensions.Logging;

namespace Cratis.Arc.EntityFrameworkCore.Observe.for_EntityChangeTracker.given;

public class an_entity_change_tracker : Specification
{
    protected ILogger<EntityChangeTracker> _logger;
    protected EntityChangeTracker _changeTracker;

    void Establish()
    {
        _logger = Substitute.For<ILogger<EntityChangeTracker>>();
        _changeTracker = new EntityChangeTracker(_logger);
    }
}
