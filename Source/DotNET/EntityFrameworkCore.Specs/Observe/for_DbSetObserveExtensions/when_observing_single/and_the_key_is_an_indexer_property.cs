// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reactive.Linq;
using Cratis.Arc.EntityFrameworkCore.Observe.for_DbSetObserveExtensions.given;
using Microsoft.EntityFrameworkCore;

namespace Cratis.Arc.EntityFrameworkCore.Observe.for_DbSetObserveExtensions.when_observing_single;

public class and_the_key_is_an_indexer_property : a_db_set_observe_context
{
    Dictionary<string, object?> _received;

    void Establish()
    {
        _dbContext.PropertyBags.Add(new Dictionary<string, object?> { ["Id"] = 42, ["Name"] = "target" });
        _dbContext.SaveChanges();
    }

    void Because()
    {
        var subject = _dbContext.PropertyBags.ObserveSingle();
        _received = subject.FirstAsync().Timeout(TimeSpan.FromSeconds(10)).Wait();
        subject.OnCompleted();
    }

    [Fact] void should_return_the_entity() => _received["Name"].ShouldEqual("target");
}
