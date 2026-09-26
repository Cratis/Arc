// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reactive.Linq;
using Cratis.Arc.EntityFrameworkCore.Observe.for_DbSetObserveExtensions.given;
using Microsoft.EntityFrameworkCore;

namespace Cratis.Arc.EntityFrameworkCore.Observe.for_DbSetObserveExtensions.when_observing;

public class and_a_shadow_key_has_a_clr_id : a_db_set_observe_context
{
    IEnumerable<ShadowKeyWithIdEntity> _collection;
    ShadowKeyWithIdEntity _single;
    ShadowKeyWithIdEntity _byId;

    void Establish()
    {
        var entity = new ShadowKeyWithIdEntity { Id = 7, Name = "target" };
        _dbContext.ShadowKeyWithIdEntities.Add(entity);
        _dbContext.Entry(entity).Property("ShadowId").CurrentValue = 42;
        _dbContext.SaveChanges();
    }

    void Because()
    {
        var collectionSubject = _dbContext.ShadowKeyWithIdEntities.Observe();
        _collection = collectionSubject.FirstAsync().Timeout(TimeSpan.FromSeconds(10)).Wait();
        collectionSubject.OnCompleted();

        var singleSubject = _dbContext.ShadowKeyWithIdEntities.ObserveSingle();
        _single = singleSubject.FirstAsync().Timeout(TimeSpan.FromSeconds(10)).Wait();
        singleSubject.OnCompleted();

        var byIdSubject = _dbContext.ShadowKeyWithIdEntities.ObserveById(7);
        _byId = byIdSubject.FirstAsync().Timeout(TimeSpan.FromSeconds(10)).Wait();
        byIdSubject.OnCompleted();
    }

    [Fact] void should_observe_the_collection() => _collection.Single().Name.ShouldEqual("target");
    [Fact] void should_observe_a_single_entity() => _single.Name.ShouldEqual("target");
    [Fact] void should_observe_by_clr_id() => _byId.Name.ShouldEqual("target");
}
