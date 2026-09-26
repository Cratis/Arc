// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reactive.Linq;
using Cratis.Arc.EntityFrameworkCore.Observe.for_DbSetObserveExtensions.given;
using Microsoft.EntityFrameworkCore;

namespace Cratis.Arc.EntityFrameworkCore.Observe.for_DbSetObserveExtensions.when_observing;

public class and_an_indexer_key_entity_is_inserted : a_db_set_observe_context
{
    Dictionary<string, object?>[] _received;

    void Because()
    {
        var subject = _dbContext.PropertyBags.Observe();
        using var subscription = subject.Subscribe(entities => _received = entities.ToArray());
        _dbContext.PropertyBags.Add(new Dictionary<string, object?> { ["Id"] = 42, ["Name"] = "inserted" });
        _dbContext.SaveChanges();
        subject.OnCompleted();
    }

    [Fact] void should_refresh_the_shared_type_set() => _received.Single()["Name"].ShouldEqual("inserted");
}
