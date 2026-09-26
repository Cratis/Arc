// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.EntityFrameworkCore.Observe.for_DbSetObserveExtensions.given;
using Microsoft.EntityFrameworkCore;

namespace Cratis.Arc.EntityFrameworkCore.Observe.for_DbSetObserveExtensions;

public class when_selecting_observation_overloads : Specification
{
    Action<DbSet<TestEntity>> _calls;

    void Because() => _calls = dbSet =>
    {
        _ = dbSet.Observe();
        _ = dbSet.Observe(entity => entity.IsActive);
        _ = dbSet.Observe(entity => entity.IsActive, configure: set => set);
        _ = dbSet.Observe(ignoreQueryContext: true);
        _ = dbSet.Observe(entity => entity.IsActive, ignoreQueryContext: true, configure: set => set);
        _ = dbSet.Observe(ignoreQueryContext: true, configure: set => set);

        _ = dbSet.ObserveSingle();
        _ = dbSet.ObserveSingle(entity => entity.IsActive, configure: set => set);
        _ = dbSet.ObserveSingle(ignoreQueryContext: true);
        _ = dbSet.ObserveSingle(entity => entity.IsActive, ignoreQueryContext: true, configure: set => set);
        _ = dbSet.ObserveSingle(ignoreQueryContext: true, configure: set => set);

        _ = dbSet.ObserveById(1);
        _ = dbSet.ObserveById(1, configure: set => set);
        _ = dbSet.ObserveById(1, ignoreQueryContext: true, configure: set => set);
    };

    [Fact] void should_compile_existing_and_context_free_call_shapes() => _calls.ShouldNotBeNull();
}
