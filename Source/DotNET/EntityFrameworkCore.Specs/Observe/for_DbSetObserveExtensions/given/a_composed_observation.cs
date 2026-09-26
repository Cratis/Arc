// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Queries;
using Cratis.Execution;
using Microsoft.Extensions.DependencyInjection;

namespace Cratis.Arc.EntityFrameworkCore.Observe.for_DbSetObserveExtensions.given;

public class a_composed_observation : a_db_set_observe_context
{
    protected AuxiliaryEntity _target;

    void Establish()
    {
        SeedTestData(
            new TestEntity { Name = "Primary A" },
            new TestEntity { Name = "Primary B" },
            new TestEntity { Name = "Primary C" },
            new TestEntity { Name = "Primary D" });
        _target = new AuxiliaryEntity { Name = "Auxiliary A", IsActive = true };
        _dbContext.AuxiliaryEntities.AddRange(
            _target,
            new AuxiliaryEntity { Name = "Auxiliary B", IsActive = true },
            new AuxiliaryEntity { Name = "Auxiliary C", IsActive = false });
        _dbContext.SaveChanges();
        _queryContext = new QueryContext("[Test]", CorrelationId.New(), new Paging(1, 1, true), new Sorting("Name", SortDirection.Descending));
    }

    protected void InsertAuxiliary(string name, bool isActive)
    {
        using var scope = _serviceProvider.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<TestDbContext>();
        context.AuxiliaryEntities.Add(new AuxiliaryEntity { Name = name, IsActive = isActive });
        context.SaveChanges();
    }
}
