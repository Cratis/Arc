// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reactive.Linq;
using Cratis.Arc.EntityFrameworkCore.Observe.for_DbSetObserveExtensions.given;
using Microsoft.EntityFrameworkCore;

namespace Cratis.Arc.EntityFrameworkCore.Observe.for_DbSetObserveExtensions.when_observing_by_id;

public class and_the_primary_key_is_not_id : a_db_set_observe_context
{
    SeparateKeyEntity _received;

    void Establish()
    {
        _dbContext.SeparateKeyEntities.AddRange(
            new SeparateKeyEntity { Key = 7, Id = 42, Name = "other" },
            new SeparateKeyEntity { Key = 42, Id = 7, Name = "target" });
        _dbContext.SaveChanges();
    }

    void Because()
    {
        var subject = _dbContext.SeparateKeyEntities.ObserveById(7);
        _received = subject.FirstAsync().Timeout(TimeSpan.FromSeconds(10)).Wait();
        subject.OnCompleted();
    }

    [Fact] void should_filter_on_the_mapped_clr_id() => _received.Name.ShouldEqual("target");
}
