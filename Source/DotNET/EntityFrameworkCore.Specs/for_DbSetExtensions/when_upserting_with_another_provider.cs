// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.EntityFrameworkCore.for_DbSetExtensions.Model;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;

namespace Cratis.Arc.EntityFrameworkCore.for_DbSetExtensions;

public class when_upserting_with_another_provider : given.an_upsert_database
{
    EntityState _state;
    int _countBeforeSave;
    int _countAfterSave;

    async Task Because()
    {
        var options = new DbContextOptionsBuilder<UpsertDbContext>(_options)
            .ReplaceService<IDatabaseProvider, given.OtherDatabaseProvider>().Options;
        await using var context = new UpsertDbContext(options);
        await context.Items.UpsertAsync(_item);
        _state = context.Entry(_item).State;
        _countBeforeSave = await context.Items.CountAsync();
        await context.SaveChangesAsync();
        _countAfterSave = await context.Items.CountAsync();
    }

    [Fact] void should_fall_back_to_a_tracked_insert() => _state.ShouldEqual(EntityState.Added);
    [Fact] void should_defer_persistence() => _countBeforeSave.ShouldEqual(0);
    [Fact] void should_persist_on_save_changes() => _countAfterSave.ShouldEqual(1);
}
