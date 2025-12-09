// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.EntityFrameworkCore.Observe.for_EntityChangeTracker.given;

public class an_entity_change_tracker : Specification
{
    protected EntityChangeTracker _changeTracker;

    void Establish()
    {
        _changeTracker = new EntityChangeTracker();
    }
}
